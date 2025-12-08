//using Microsoft.AspNetCore.Identity;
//using ProjectManagementSystem.Models;
//using System;
//using System.Threading.Tasks;
//using Microsoft.Extensions.DependencyInjection;
//using ProjectManagementSystem.DBModels;

//public static class IdentitySeeder
//{
//    public static async Task SeedRolesAndAdminsAsync(IServiceProvider serviceProvider)
//    {
//        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
//        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

//        string[] roles = { "Admin", "User" };

//        // Create roles if they don't exist
//        foreach (var role in roles)
//        {
//            if (!await roleManager.RoleExistsAsync(role))
//            {
//                await roleManager.CreateAsync(new IdentityRole(role));
//            }
//        }

//        var admins = new[]
//        {
//            new { Email = "admin1@example.com", Password = "Admin@1", FullName = "Admin One" },
//            new { Email = "admin2@example.com", Password = "Admin@2", FullName = "Admin Two" }
//        };

//        foreach (var admin in admins)
//        {
//            var user = await userManager.FindByEmailAsync(admin.Email);

//            if (user == null)
//            {
//                // Create new admin with default password
//                var newAdmin = new ApplicationUser
//                {
//                    FullName = admin.FullName,
//                    UserName = admin.Email,
//                    Email = admin.Email,
//                    EmailConfirmed = true,
//                    IsUsingDefaultPassword = true
//                };

//                var result = await userManager.CreateAsync(newAdmin, admin.Password);
//                if (result.Succeeded)
//                {
//                    await userManager.AddToRoleAsync(newAdmin, "Admin");
//                    Console.WriteLine($"Admin created: {admin.Email}");
//                }
//            }
//            else
//            {
//                // Only update the name - DON'T reset the password
//                user.FullName = admin.FullName;

//                // Only set IsUsingDefaultPassword if it's a new account
//                // Remove this line if you don't want to modify this flag for existing users
//                // user.IsUsingDefaultPassword = true;

//                await userManager.UpdateAsync(user);

//                Console.WriteLine($"Admin updated (password not changed): {admin.Email}");
//            }
//        }
//    }
//}