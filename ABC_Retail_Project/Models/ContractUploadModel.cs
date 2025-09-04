using System.ComponentModel.DataAnnotations;

namespace ABC_Retail_Project.Models
{
    public class ContractUploadModel
    {
        [Required(ErrorMessage = "Customer ID is required")]
        public string CustomerId { get; set; }

        [Required(ErrorMessage = "Please select a file")]
        public IFormatProvider ContractFile { get; set; }
    }
}
