using MagicVilla_VillaAPI.Data;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MagicVilla_VillaAPI.Controllers
{
    [Authorize]
    [Route("api/VillaAPI")]
    [ApiController]
    public class VillaAPIController : ControllerBase
    {
        public readonly ApplicationDbContext _db;

        public VillaAPIController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<VillaDTO>> GetVillas()
        {
            return Ok(_db.Villas.ToList());
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id:int}", Name = "GetVilla")] 
        public ActionResult<VillaDTO> GetVilla(int id)
        {
            if (id == 0)
            {
                return BadRequest();
            }
            var villa = _db.Villas.FirstOrDefault(u => u.Id == id);

            if (villa == null)
            {
                return NotFound();
            }
            return Ok(villa);
        }




        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult<VillaDTO> CreateVilla([FromBody] VillaDTO villaDTO)
        {
            if (villaDTO == null)
            {
                return BadRequest(villaDTO);
            }

            if (_db.Villas.Any(u => u.Name.ToLower() == villaDTO.Name.ToLower()))
            {
                ModelState.AddModelError("CustomError", "Villa already exists");
                return BadRequest(ModelState);
            }

            if (villaDTO.Id > 0)
            {
                return BadRequest("Id should not be provided for new records.");
            }

            Villa model = new Villa()
            {
                Amenity = villaDTO.Amenity,
                Details = villaDTO.Details,
                Id = villaDTO.Id,
                ImageUrl = villaDTO.ImageUrl,
                Name = villaDTO.Name,
                Occupancy = villaDTO.Occupancy,
                Rate = villaDTO.Rate,
                Sqft = villaDTO.Sqft
            };

            _db.Villas.Add(model);
            _db.SaveChanges();
            return CreatedAtRoute("GetVilla", new { id = model.Id }, villaDTO);
        }




        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}", Name = "DeleteVilla")]
       public IActionResult DeleteVilla(int id)
        {
            if (id == 0)
            {
                return BadRequest();
            }
            var villa = _db.Villas.FirstOrDefault(u => u.Id == id);

            if (villa == null)
            {
                return NotFound();
            }

            _db.Villas.Remove(villa);
            _db.SaveChanges();
            return NoContent();
        }




        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}", Name = "UpdateVilla")]

        public IActionResult UpdateVilla(int id, [FromBody] VillaDTO villaDTO)
        {
            if (villaDTO == null || id != villaDTO.Id)
            {
                return BadRequest();
            }

            Villa model = new Villa()
            {
                Amenity = villaDTO.Amenity,
                Details = villaDTO.Details,
                Id = villaDTO.Id,
                ImageUrl = villaDTO.ImageUrl,
                Name = villaDTO.Name,
                Occupancy = villaDTO.Occupancy,
                Rate = villaDTO.Rate,
                Sqft = villaDTO.Sqft
            };

            _db.Update(model);
            _db.SaveChanges();
            return NoContent();
        }



        [Authorize(Roles = "Admin")]
        [HttpPatch("{id:int}", Name = "UpdatePartialVilla")]

        public IActionResult UpdatePartialVilla(int id, JsonPatchDocument<VillaDTO> patchDto)
        {
            if (patchDto == null || id == 0)
            {
                return BadRequest();
            }

            var villa = _db.Villas.AsNoTracking().FirstOrDefault(u => u.Id == id);
            if (villa == null)
            {
                return NotFound();
            }

            VillaDTO villaDTO = new VillaDTO()
            {
                Amenity = villa.Amenity,
                Details = villa.Details,
                Id = villa.Id,
                ImageUrl = villa.ImageUrl,
                Name = villa.Name,
                Occupancy = villa.Occupancy,
                Rate = villa.Rate,
                Sqft = villa.Sqft
            };

            patchDto.ApplyTo(villaDTO, ModelState);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Villa model = new Villa()
            {
                Amenity = villaDTO.Amenity,
                Details = villaDTO.Details,
                Id = villaDTO.Id,
                ImageUrl = villaDTO.ImageUrl,
                Name = villaDTO.Name,
                Occupancy = villaDTO.Occupancy,
                Rate = villaDTO.Rate,
                Sqft = villaDTO.Sqft
            };

            _db.Update(model);
            _db.SaveChanges();
            return NoContent();
        }
    }
}
