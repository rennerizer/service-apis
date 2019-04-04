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
        public IActionResult GetAuthors(AuthorsResourceParameters parameters, [FromHeader(Name = "Accept")] string mediaType)
        {
            if (!_mappingService.ValidMappingExistsFor<AuthorDto, Author>(parameters.OrderBy))
                return BadRequest();

            if (!_typeService.TypeHasProperties<AuthorDto>(parameters.Fields))
                return BadRequest();

            var authorEntities = _repository.GetAuthors(parameters);

            //var previousPageLink = authorEntities.HasPrevious ?
            //    CreateAuthorsResourceUri(parameters, ResourceUriType.PreviousPage) : null;

            //var nextPageLink = authorEntities.HasNext ?
            //    CreateAuthorsResourceUri(parameters, ResourceUriType.NextPage) : null;

            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorEntities);

            if (mediaType == "application/vnd.marvin.hateoas+json")
            {
                var paginationMetadata = new
                {
                    totalCount = authorEntities.TotalCount,
                    pageSize = authorEntities.PageSize,
                    currentPage = authorEntities.CurrentPage,
                    totalPages = authorEntities.TotalPages
                };

                Response.Headers.Add("X-Pagination", Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));

                var links = CreateLinksForAuthors(parameters, authorEntities.HasNext, authorEntities.HasPrevious);

                var shapedAuthors = authors.ShapeData(parameters.Fields);

                var shapedAuthorsWithLinks = shapedAuthors.Select(author =>
                {
                    var authorAsDictionary = author as IDictionary<string, object>;
                    var authorLinks = CreateLinksForAuthor((Guid)authorAsDictionary["Id"], parameters.Fields);

                    authorAsDictionary.Add("links", authorLinks);

                    return authorAsDictionary;
                });

                var linkedCollectionResource = new
                {
                    value = shapedAuthorsWithLinks,
                    links = links
                };

                return Ok(linkedCollectionResource);
            }
            else
            {
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

                Response.Headers.Add("X-Pagination", Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));

                return Ok(authors.ShapeData(parameters.Fields));
            }
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
                case ResourceUriType.Current:
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

            var links = CreateLinksForAuthor(id, fields);

            var linkedResourceToReturn = author.ShapeData(fields) as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            return Ok(linkedResourceToReturn);
        }

        [HttpPost(Name = "CreateAuthor")]
        [RequestHeaderMatchesMediaType("Content-Type", new[] { "application/vnd.marvin.author.full+json" })]
        public IActionResult CreateAuthor([FromBody] AuthorForCreationDto author)
        {
            if (author == null)
                return BadRequest();

            var authorEntity = Mapper.Map<Author>(author);

            _repository.AddAuthor(authorEntity);

            if (!_repository.Save())
                throw new Exception("Creating an author failed on save.");

            var authorToReturn = Mapper.Map<AuthorDto>(authorEntity);

            var links = CreateLinksForAuthor(authorToReturn.Id, null);

            var linkedResourceToReturn = authorToReturn.ShapeData(null) as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            return CreatedAtRoute("GetAuthor", new { id = linkedResourceToReturn["Id"] }, linkedResourceToReturn);
        }

        [HttpPost(Name = "CreateAuthorWithDateOfDeath")]
        [RequestHeaderMatchesMediaType("Content-Type", new[] { "application/vnd.marvin.authorwithdateofdeath.full+json", "application/vnd.marvin.authorwithdateofdeath.full+xml" })]
        public IActionResult CreateAuthorWithDateOfDeathDto([FromBody] AuthorForCreationWithDateOfDeathDto author)
        {
            if (author == null)
                return BadRequest();

            var authorEntity = Mapper.Map<Author>(author);

            _repository.AddAuthor(authorEntity);

            if (!_repository.Save())
                throw new Exception("Creating an author failed on save.");

            var authorToReturn = Mapper.Map<AuthorDto>(authorEntity);

            var links = CreateLinksForAuthor(authorToReturn.Id, null);

            var linkedResourceToReturn = authorToReturn.ShapeData(null) as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            return CreatedAtRoute("GetAuthor", new { id = linkedResourceToReturn["Id"] }, linkedResourceToReturn);
        }

        [HttpPost("{id}")]
        public IActionResult BlockAuthorCreation(Guid id)
        {
            if (_repository.AuthorExists(id))
                return new StatusCodeResult(StatusCodes.Status409Conflict);

            return NotFound();
        }

        [HttpDelete("{id}", Name = "DeleteAuthor")]
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

        private IEnumerable<LinkDto> CreateLinksForAuthor(Guid id, string fields)
        {
            var links = new List<LinkDto>();

            if (string.IsNullOrWhiteSpace(fields))
                links.Add(new LinkDto(_urlHelper.Link("GetAuthor", new { id = id }), "self", "GET"));
            else
                links.Add(new LinkDto(_urlHelper.Link("GetAuthor", new { id = id, fields = fields }), "self", "GET"));

            links.Add(new LinkDto(_urlHelper.Link("DeleteAuthor", new { id = id }), "delete_author", "DELETE"));

            links.Add(new LinkDto(_urlHelper.Link("CreateBookForAuthor", new { authorId = id }), "create_book_for_author", "POST"));

            links.Add(new LinkDto(_urlHelper.Link("GetBooksForAuthor", new { authorId = id }), "books", "GET"));

            return links;
        }

        private IEnumerable<LinkDto> CreateLinksForAuthors(AuthorsResourceParameters paramters, bool hasNext, bool hasPrevious)
        {
            var links = new List<LinkDto>();

            links.Add(new LinkDto(CreateAuthorsResourceUri(paramters, ResourceUriType.Current), "self", "GET"));

            if (hasNext)
                links.Add(new LinkDto(CreateAuthorsResourceUri(paramters, ResourceUriType.NextPage), "nextPage", "GET"));

            if (hasPrevious)
                links.Add(new LinkDto(CreateAuthorsResourceUri(paramters, ResourceUriType.PreviousPage), "previousPage", "GET"));

            return links;
        }
    }
}
