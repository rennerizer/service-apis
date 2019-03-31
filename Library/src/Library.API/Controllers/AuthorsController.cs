using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Library.API.Entities;
using Library.API.Models;
using Library.API.Services;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Library.API.Helpers;

namespace Library.API.Controllers
{
    [Route("api/authors")]
    public class AuthorsController : Controller
    {
        private ILibraryRepository _repository;
        private IUrlHelper _urlHelper;
        private IPropertyMappingService _mappingService;
        private ITypeHelperService _typeService;

        public AuthorsController(
            ILibraryRepository repository, 
            IUrlHelper urlHelper, 
            IPropertyMappingService mappingService,
            ITypeHelperService typeService)
        {
            _repository = repository;
            _urlHelper = urlHelper;
            _mappingService = mappingService;
            _typeService = typeService;
        }

        [HttpGet(Name = "GetAuthors")]
        public IActionResult GetAuthors(AuthorsResourceParameters parameters)
        {
            if (!_mappingService.ValidMappingExistsFor<AuthorDto, Author>(parameters.OrderBy))
                return BadRequest();

            if (!_typeService.TypeHasProperties<AuthorDto>(parameters.Fields))
                return BadRequest();

            var authorEntities = _repository.GetAuthors(parameters);

            var previousPageLink = authorEntities.HasPrevious ?
                CreateAuthorsResourceUri(parameters, ResourceUriType.PreviousPage) : null;

            var nextPageLink = authorEntities.HasNext ?
                CreateAuthorsResourceUri(parameters, ResourceUriType.NextPage) : null;

            var paginationMetadata = new
            {
                totalCount = authorEntities.TotalCount,
                pageSize = authorEntities.PageSize,
                currentPage = authorEntities.CurrentPage,
                totalPages = authorEntities.TotalPages,
                previousPageLink = previousPageLink,
                nextPageLink = nextPageLink
            };

            Response.Headers.Add("X-Pagination",
                Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));

            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorEntities);

            return Ok(authors.ShapeData(parameters.Fields));
        }

        private string CreateAuthorsResourceUri(AuthorsResourceParameters parameters, ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return _urlHelper.Link("GetAuthors",
                        new
                        {
                            fields = parameters.Fields,
                            orderBy = parameters.OrderBy,
                            searchQuery = parameters.SearchQuery,
                            genre = parameters.Genre,
                            pageNumber = parameters.PageNumber - 1,
                            pageSize = parameters.PageSize
                        });
                case ResourceUriType.NextPage:
                    return _urlHelper.Link("GetAuthors",
                        new
                        {
                            fields = parameters.Fields,
                            orderBy = parameters.OrderBy,
                            searchQuery = parameters.SearchQuery,
                            genre = parameters.Genre,
                            pageNumber = parameters.PageNumber + 1,
                            pageSize = parameters.PageSize
                        });
                default:
                    return _urlHelper.Link("GetAuthors",
                        new
                        {
                            fields = parameters.Fields,
                            orderBy = parameters.OrderBy,
                            searchQuery = parameters.SearchQuery,
                            genre = parameters.Genre,
                            pageNumber = parameters.PageNumber,
                            pageSize = parameters.PageSize
                        });
            }
        }

        [HttpGet("{id}", Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid id, [FromQuery] string fields)
        {
            if (!_typeService.TypeHasProperties<AuthorDto>(fields))
                return BadRequest();

            var authorEntity = _repository.GetAuthor(id);

            if (authorEntity == null)
                return NotFound();

            var author = Mapper.Map<AuthorDto>(authorEntity);

            return Ok(author.ShapeData(fields));
        }

        [HttpPost]
        public IActionResult CreateAuthor([FromBody] AuthorForCreationDto author)
        {
            if (author == null)
                return BadRequest();

            var authorEntity = Mapper.Map<Author>(author);

            _repository.AddAuthor(authorEntity);

            if (!_repository.Save())
                throw new Exception("Creating an author failed on save.");

            var authorToReturn = Mapper.Map<AuthorDto>(authorEntity);

            return CreatedAtRoute("GetAuthor", new { id = authorToReturn.Id }, authorToReturn);
        }

        [HttpPost("{id}")]
        public IActionResult BlockAuthorCreation(Guid id)
        {
            if (_repository.AuthorExists(id))
                return new StatusCodeResult(StatusCodes.Status409Conflict);

            return NotFound();
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteAuthor(Guid id)
        {
            var authorFromRepo = _repository.GetAuthor(id);

            if (authorFromRepo == null)
                return NotFound();

            _repository.DeleteAuthor(authorFromRepo);

            if (!_repository.Save())
                throw new Exception($"Deleting author {id} failed on save.");

            return NoContent();
        }
    }
}
