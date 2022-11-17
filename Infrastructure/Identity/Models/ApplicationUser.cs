using Mcrio.AspNetCore.Identity.On.RavenDb.Model.User;

namespace Platform.Infrastructure.Identity.Models;

public class ApplicationUser : RavenIdentityUser
{
    public string FirstName { get; set; }

    public string LastName { get; set; }
}
