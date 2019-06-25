using System.ComponentModel.DataAnnotations;

namespace Zen.Module.Web.REST.Services.Identity.AccountViewModels
{
    public class ExternalLoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}