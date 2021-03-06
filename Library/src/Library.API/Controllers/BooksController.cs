﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

using Library.API.Services;
using Library.API.Models;
using Library.API.Entities;
using Library.API.Helpers;

using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        private ILibraryRepository _repository;
        private ILogger<BooksController> _logger;
        private IUrlHelper _urlHelper;

        public BooksController(ILibraryRepository repository, ILogger<BooksController> logger, IUrlHelper urlHelper)
        {
            _repository = repository;
            _logger = logger;
            _urlHelper = urlHelper;
        }

        [HttpGet(Name ="GetBooksForAuthor")]
        public IActionResult GetBooksForAuthor(Guid authorId)
        {
            if (!_repository.AuthorExists(authorId))
                return NotFound();

            var bookEntities = _repository.GetBooksForAuthor(authorId);

            var books = Mapper.Map<IEnumerable<BookDto>>(bookEntities);

            books = books.Select(book =>
            {
                book = CreateLinksForBook(book);
                return book;
            });

            var wrapper = new LinkedCollectionResourceWrapperDto<BookDto>(books);

            return Ok(CreateLinksForBooks(wrapper));
        }

        [HttpGet("{id}", Name ="GetBookForAuthor")]
        public IActionResult GetBookForAuthor(Guid authorId, Guid id)
        {
            if (!_repository.AuthorExists(authorId))
                return NotFound();

            var bookEntity = _repository.GetBookForAuthor(authorId, id);

            if (bookEntity == null)
                return NotFound();

            var book = Mapper.Map<BookDto>(bookEntity);

            return Ok(CreateLinksForBook(book));
        }

        [HttpPost(Name = "CreateBookForAuthor")]
        public IActionResult CreateBookForAuthor(Guid authorId, [FromBody] BookForCreationDto book)
        {
            if (book == null)
                return BadRequest();

            if (book.Title == book.Description)
                ModelState.AddModelError(nameof(BookForCreationDto), "The provided description should be different from the title.");

            if (!ModelState.IsValid)
                return new UnprocessableEntityObjectResult(ModelState);

            if (!_repository.AuthorExists(authorId))
                return NotFound();

            var bookEntity = Mapper.Map<Book>(book);

            _repository.AddBookForAuthor(authorId, bookEntity);

            if (!_repository.Save())
                throw new Exception($"Creating a book for author {authorId} failed on save.");

            var bookToReturn = Mapper.Map<BookDto>(bookEntity);

            return CreatedAtRoute("GetBookForAuthor", new { authorId = authorId, id = bookToReturn.Id }, CreateLinksForBook(bookToReturn));
        }

        [HttpDelete("{id}", Name = "DeleteBookForAuthor")]
        public IActionResult DeleteBookForAuthor(Guid authorId, Guid id)
        {
            if (!_repository.AuthorExists(authorId))
                return NotFound();

            var bookForAuthorFromRepo = _repository.GetBookForAuthor(authorId, id);

            if (bookForAuthorFromRepo == null)
                return NotFound();

            _repository.DeleteBook(bookForAuthorFromRepo);

            if (!_repository.Save())
                throw new Exception($"Deleting book {id} for author {authorId} failed on save.");

            _logger.LogInformation(100, $"Book {id} for author {authorId} was deleted.");

            return NoContent();
        }

        [HttpPut("{id}", Name = "UpdateBookForAuthor")]
        public IActionResult UpdateBookForAuthor(Guid authorId, Guid id, [FromBody] BookForUpdateDto book)
        {
            if (book == null)
                return BadRequest();

            if (book.Title == book.Description)
                ModelState.AddModelError(nameof(BookForUpdateDto), "The provided description should be different from the title.");

            if (!ModelState.IsValid)
                return new UnprocessableEntityObjectResult(ModelState);

            if (!_repository.AuthorExists(authorId))
                return NotFound();

            var bookForAuthorFromRepo = _repository.GetBookForAuthor(authorId, id);

            if (bookForAuthorFromRepo == null)
            {
                var bookToAdd = Mapper.Map<Book>(book);
                bookToAdd.Id = id;

                _repository.AddBookForAuthor(authorId, bookToAdd);

                if (!_repository.Save())
                    throw new Exception($"Upserting book {id} for author {authorId} failed on save.");

                var bookToReturn = Mapper.Map<BookDto>(bookToAdd);

                return CreatedAtRoute("GetBookForAuthor", new { authorId = authorId, id = bookToReturn.Id }, bookToReturn);
            }

            Mapper.Map(book, bookForAuthorFromRepo);

            _repository.UpdateBookForAuthor(bookForAuthorFromRepo);

            if (!_repository.Save())
                throw new Exception($"Updated a book {id} for author {authorId} failed on save.");

            return NoContent();
        }

        [HttpPatch("{id}", Name = "PartiallyUpdateBookForAuthor")]
        public IActionResult PartiallyUpdateBookForAuthor(Guid authorId, Guid id, [FromBody] JsonPatchDocument<BookForUpdateDto> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest();

            if (!_repository.AuthorExists(authorId))
                return NotFound();

            var bookForAuthorFromRepo = _repository.GetBookForAuthor(authorId, id);

            if (bookForAuthorFromRepo == null)
            {
                var bookDto = new BookForUpdateDto();

                patchDoc.ApplyTo(bookDto, ModelState);

                if (bookDto.Title == bookDto.Description)
                    ModelState.AddModelError(nameof(BookForUpdateDto), "The provided description should be different from the title.");

                TryValidateModel(bookDto);

                if (!ModelState.IsValid)
                    return new UnprocessableEntityObjectResult(ModelState);

                var bookToAdd = Mapper.Map<Book>(bookDto);
                bookToAdd.Id = id;

                _repository.AddBookForAuthor(authorId, bookToAdd);

                if (!_repository.Save())
                    throw new Exception($"Upserting book {id} for author {authorId} failed on save.");

                var bookToReturn = Mapper.Map<BookDto>(bookToAdd);

                return CreatedAtRoute("GetBookForAuthor", new { authorId = authorId, id = bookToReturn.Id }, bookToReturn);
            }
                
            var bookToPatch = Mapper.Map<BookForUpdateDto>(bookForAuthorFromRepo);

            patchDoc.ApplyTo(bookToPatch, ModelState);
            //patchDoc.ApplyTo(bookToPatch);

            if (bookToPatch.Title == bookToPatch.Description)
                ModelState.AddModelError(nameof(BookForUpdateDto), "The provided description should be different from the title.");

            TryValidateModel(bookToPatch);

            if (!ModelState.IsValid)
                return new UnprocessableEntityObjectResult(ModelState);

            Mapper.Map(bookToPatch, bookForAuthorFromRepo);

            _repository.UpdateBookForAuthor(bookForAuthorFromRepo);

            if (!_repository.Save())
                throw new Exception($"Patching book {id} for author {authorId} failed on save.");

            return NoContent();
        }

        private BookDto CreateLinksForBook(BookDto book)
        {
            book.Links.Add(new LinkDto(_urlHelper.Link("GetBookForAuthor", new { id = book.Id }), "self", "GET"));

            book.Links.Add(new LinkDto(_urlHelper.Link("DeleteBookForAuthor", new { id = book.Id }), "delete_book", "DELETE"));

            book.Links.Add(new LinkDto(_urlHelper.Link("UpdateBookForAuthor", new { id = book.Id }), "update_book", "PUT"));

            book.Links.Add(new LinkDto(_urlHelper.Link("PartiallyUpdateBookForAuthor", new { id = book.Id }), "partially_update_book", "PATCH"));

            return book;
        }

        private LinkedCollectionResourceWrapperDto<BookDto> CreateLinksForBooks(LinkedCollectionResourceWrapperDto<BookDto> booksWrapper)
        {
            booksWrapper.Links.Add(new LinkDto(_urlHelper.Link("GetBooksForAuthor", new { }), "self", "GET"));

            return booksWrapper;
        }
    }
}
