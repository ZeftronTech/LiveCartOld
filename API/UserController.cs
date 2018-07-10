using LiveKart.Entities;
using LiveKart.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace LiveKart.Web.API
{
	public class UserController : ApiController
	{

		private IUserService _userService;

		public UserController(IUserService userService)
		{
			_userService = userService;
		}

		// GET api/<controller>/5
		public bool Get(string id)
		{


			var user = _userService.IsUserExist(id) ;

			return user;

		}

	}
}
