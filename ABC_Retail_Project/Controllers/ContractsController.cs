using ABC_Retail_Project.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail_Project.Controllers
{
    public class ContractsController : Controller
    {
        private readonly ContractService _contractService;

        public ContractsController(ContractService contractService)
        {
            _contractService = contractService;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile contractFile, string customerId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _contractService.UploadContractAsync(contractFile, customerId);
                    TempData["Message"] = "Contract uploaded successfully!";
                    return RedirectToAction(nameof(Upload));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error uploading file: {ex.Message}");
                }
            }
            return View();
        }
    }
}