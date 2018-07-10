using LiveKart.Entities;
using Repository.Pattern.UnitOfWork;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;


namespace LiveKart.Web
{
	public class CountryController : ApiController
	{

		private readonly IUnitOfWork _unitofwork;

		public CountryController(IUnitOfWork unitofwork)
		{
			_unitofwork = unitofwork;
		}



		/// <summary>
		/// List of countries
		/// </summary>
		/// <returns>List{Country}.</returns>
		[HttpGet, ActionName("All")]
		[ResponseType(typeof(IEnumerable<Country>))]
		public IHttpActionResult All()
		{
			var result = _unitofwork.Repository<Country>().Query().Select();
			return Ok<IEnumerable<Country>>(result);
		}


		/// <summary>
		/// List of countries
		/// </summary>
		/// <returns>List{Country}.</returns>
		[HttpGet, ActionName("Get")]
		[ResponseType(typeof(Country))]
		public IHttpActionResult GetCountries(long CountryID)
		{
			var result = _unitofwork.Repository<Country>().Find(CountryID);
			return Ok(result);
		}
	}
}
