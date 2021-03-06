﻿using Microsoft.AspNetCore.Mvc;
using SIL.Machine.WebApi.Dtos;

namespace SIL.Machine.WebApi.Server.Controllers
{
	internal static class ControllersExtensions
	{
		public static string GetEntityUrl(this IUrlHelper urlHelper, string routeName, string id)
		{
			return urlHelper.RouteUrl(routeName) + $"/id:{id}";
		}

		public static ResourceDto CreateLinkDto(this IUrlHelper urlHelper, string routeName, string id)
		{
			return new ResourceDto
			{
				Id = id,
				Href = urlHelper.GetEntityUrl(routeName, id)
			};
		}
	}
}

