using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Library.API.Models;
using Library.API.Services;
using Library.API.Helpers;
using AutoMapper;

namespace Library.API.Controllers
{
    [Route("api/authors")]
    public class AuthorsController : Controller
    {
        private ILibraryRepository _repository;

        public AuthorsController(ILibraryRepository repository)
        {
            _repository = repository;
        }

        [HttpGet()]
        public IActionResult GetAuthors()
        {
            var authorEntities = _repository.GetAuthors();

            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorEntities);

            return Ok(authors);
        }

        [HttpGet("{id}")]
        public IActionResult GetAuthor(Guid id)
        {
            var authorEntity = _repository.GetAuthor(id);

            if (authorEntity == null)
                return NotFound();

            var author = Mapper.Map<AuthorDto>(authorEntity);

            return Ok(author);
        }
    }
}
