using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Query;
using LiveKart.Entities.Models;
using LiveKart.Repository;
using LiveKart.Service;
using Repository.Pattern.Infrastructure;
using Repository.Pattern.Repositories;
using Repository.Pattern.UnitOfWork;


namespace LiveKart.Web.API
{
	public class AssetController : ApiController
	{
		private readonly IUnitOfWork _unitofwork;
		private readonly IAssetService _assetService;


		public AssetController(
			IUnitOfWork unitofwork, 
			IAssetService assetService)
		{
			_unitofwork = unitofwork;
			_assetService = assetService;
		}

		// GET: api/Asset
		public IEnumerable<string> Get()
		{
			return new string[] { "value1", "value2" };
		}

		// GET: api/Asset/5
		public string Get(int id)
		{
			return "value";
		}

		// POST: api/Asset
		public void Post([FromBody]string value)
		{
		}

		// PUT: api/Asset/5
		public void Put(int id, [FromBody]string value)
		{
		}

		// DELETE: api/Asset/5
		public void Delete(int id)
		{
		}
	}
}
