using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Library.API.Services;
using AutoMapper;
using Library.API.Models;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        private ILibraryRepository _repository;

        public BooksController(ILibraryRepository repository)
        {
            _repository = repository;
        }

        [HttpGet()]
        public IActionResult GetBooksForAuthor(Guid authorId)
        {
            if (!_repository.AuthorExists(authorId))
                return NotFound();

            var bookEntities = _repository.GetBooksForAuthor(authorId);

            var books = Mapper.Map<IEnumerable<BookDto>>(bookEntities);

            return Ok(books);
        }

        [HttpGet("{id}")]
        public IActionResult GetBookForAuthor(Guid authorId, Guid id)
        {
            if (!_repository.AuthorExists(authorId))
                return NotFound();

            var bookEntity = _repository.GetBookForAuthor(authorId, id);

            if (bookEntity == null)
                return NotFound();

            var book = Mapper.Map<BookDto>(bookEntity);

            return Ok(book);
        }
    }
}
