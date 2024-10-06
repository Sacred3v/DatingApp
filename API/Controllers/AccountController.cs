using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController(DataContext context) : BaseApiController
{
    [HttpPost("register")]
    public async Task<ActionResult<AppUser>> RegisterAsync(RegisterRequest request)
    {
        if(await UserExistsAsync(request.Username)) return BadRequest("Username is taken");
        using var hmac = new HMACSHA512();
        var usr = new AppUser{
            UserName = request.Username,
            PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(request.Password)),
            PasswordSalt = hmac.Key,
        };

        context.Users.Add(usr);
        await context.SaveChangesAsync();
        return usr;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AppUser>> LoginAsync(LoginRequest request){
        var usr = await context.Users.FirstOrDefaultAsync(x => 
        x.UserName.ToLower() == request.Username.ToLower());
        
        if(usr == null) 
        return Unauthorized("Invalid username or password");

        using var hmac = new HMACSHA512(usr.PasswordSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(request.Password));

        for(int i = 0; i < computedHash.Length; i++)
            if(computedHash[i] != usr.PasswordHash[i]) 
            return Unauthorized("Invalid username or password");
        

        return usr;
    }

    private async Task<bool> UserExistsAsync(string username) => 
    await context.Users.AnyAsync(u => u.UserName.ToLower() == username.ToLower());
    
}