﻿using System;
using Kalium.Server.Context;
using Kalium.Server.Utils;
using Kalium.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Kalium.Server.Repositories;
using Kalium.Shared.Consts;
using Kalium.Shared.Conversions;
using Microsoft.AspNetCore.Authorization;
using MoreLinq;

namespace Kalium.Server.Controllers
{
    [Route("api/[controller]")]
    public class OrderController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IIdentityRepository _identityRepository;
        private readonly IAuthorizationService _authorizationService;

        public OrderController(IOrderRepository orderRepository, IIdentityRepository identityRepository, IAuthorizationService authorizationService)
        {
            _orderRepository = orderRepository;
            _identityRepository = identityRepository;
            _authorizationService = authorizationService;
        }
        
        [HttpGet("[action]")]
        [AuthorizePolicies(Consts.Policy.Checkout)]
        public async Task<string> FindOrder([FromQuery] int id)
        {
            var order = await _orderRepository.FindOrderById(id);
            var simpleOrder = order.Simplify();
            var currentUser = await _identityRepository.GetCurrentUserAsync();

            if (order == null)
            {
                return SerializeObject(new
                {
                    Code = 404
                });
            }

            var isAuthorized =
                await _authorizationService.AuthorizeAsync(HttpContext.User, Consts.Policy.ManageUser.Name());
            if (!currentUser.Id.Equals(order.User.Id) && !isAuthorized.Succeeded)
            {
                return SerializeObject(new
                {
                    Code = 403
                });
            }

            var result = new
            {
                Code = 200,
                Order = simpleOrder
            };
            return SerializeObject(result);
        }

        private static string SerializeObject(object result)
        {
            return JsonConvert.SerializeObject(result, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
        }
    }
}