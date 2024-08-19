﻿using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserService.DTOs;
using UserService.Models;
using UserService.Repo;

namespace UserService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController(IConfiguration configuration, IUserRepository userRepository, IMapper mapper) : ControllerBase
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IMapper _mapper = mapper;

        [AllowAnonymous]
        [HttpPost("users/login")]
        public ActionResult Login([FromBody] UserLoginModel userLoginModel)
        {
            try
            {
                if (userLoginModel.Email == null || userLoginModel.Password == null)
                    return BadRequest("Email or Password can't be null.");

                var roleId = _userRepository.UserCheck(userLoginModel.Email, userLoginModel.Password);
                var role = _userRepository.GetRoleById(roleId);

                var user = _mapper.Map<UserModel>(userLoginModel);
                user.RoleName = role.Name;

                var token = GenerateToken(user);

                return Ok(token);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("users/addAdmin")]
        public ActionResult AddAdmin([FromBody] UserLoginModel loginModel)
        {
            if (loginModel.Email == null || loginModel.Password == null)
                return BadRequest("Email or Password can't be null.");

            try
            {
                _userRepository.UserAdd(loginModel.Email, loginModel.Password, RoleId.Admin);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

            return CreatedAtAction(nameof(AddAdmin), new { email = loginModel.Email }, loginModel);
        }

        [HttpPost("users/addUser")]
        [Authorize(Roles = "Admin")]
        public ActionResult AddUser([FromBody] UserLoginModel loginModel)
        {
            if (loginModel.Email == null || loginModel.Password == null)
                return BadRequest("Email or Password can't be null.");

            try
            {
                _userRepository.UserAdd(loginModel.Email, loginModel.Password, RoleId.User);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

            return CreatedAtAction(nameof(AddAdmin), new { email = loginModel.Email }, loginModel);
        }

        [HttpDelete("users/deleteUser/{email}")]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteUserByEmail(string email)
        {
            try
            {
                _userRepository.DeleteUserByEmail(email);
                return Ok(email);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("users/getAllUsers")]
        [AllowAnonymous]
        public ActionResult<IEnumerable<UserModel>> GetAllUsers()
        {
            try
            {
                return Ok(_userRepository.GetUsers());
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        private string GenerateToken(UserModel user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]
                                           ?? throw new NullReferenceException("Key can't be Null")));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Email),
                new Claim(ClaimTypes.Role, user.RoleName ?? "User")
            };

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(60),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}



