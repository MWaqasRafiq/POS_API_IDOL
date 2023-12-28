using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using POS_API_IDOL.Model;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Transactions;

namespace POS_API_IDOL.Controllers
{
    //[Route("[controller]")]
    [ApiController]
    public class PosApiController : ControllerBase
    {
        private static Random RNG = new Random();
        private readonly ILogger<PosApiController> _logger;
        private static string? TransactionId;
        public static CartProducts cartProducts;
        public PosApiController(ILogger<PosApiController> logger)
        {
                _logger = logger;
        }

        [HttpPost("SignTerminal")]
        public IActionResult SignTerminal(SignTerminalRequest request)
        {
            try
            {
                if (request != null && !string.IsNullOrEmpty(request.Type))
                {
                    GenericResponse genericResponse = new GenericResponse();
                    if (request.Type.ToLower() == "on")
                    {
                        genericResponse.StatusMessage = "Terminal signed on successfully";
                        genericResponse.Status = "signed";
                        genericResponse.Code = 1;
                    }
                    else if (request.Type.ToLower() == "off")
                    {
                        genericResponse.StatusMessage = "Terminal signed off successfully";
                        genericResponse.Status = "unsigned";
                        genericResponse.Code = 0;
                    }
                    else if (request.Type.ToLower() == "status")
                    {
                        genericResponse.StatusMessage = "Terminal is signed off";
                        genericResponse.Status = "unsigned";
                        genericResponse.Code = 2;
                    }
                    else
                    {
                        return BadRequest();
                    }

                    return Ok(genericResponse);
                }
                else
                {
                    return BadRequest();
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                return Forbid();
            }
        }


        [HttpPost("StartTransaction")]
        public IActionResult StartTransaction(StartTransactionRequest request)
        {
            try
            {
                if (request != null && !string.IsNullOrEmpty(request.TerminalNo) && !string.IsNullOrEmpty(request.StoreNo))
                {
                    TransactionId = Create16DigitString();
                    StartTransactionResponse response = new StartTransactionResponse()
                    {
                        TransactionId = TransactionId,
                        Code = 1,
                        Status = "true",
                        StatusMessage = "New transaction started successfully"
                    };

                    return Ok(response);
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Forbid();
            }
        }


        [HttpPost("ProductDetails")]
        public IActionResult CheckProductDetails(CheckProductRequest request)
        {
            try
            {
                if (request != null && !string.IsNullOrEmpty(request.TerminalNo)
                    && !string.IsNullOrEmpty(request.StoreNo) && !string.IsNullOrEmpty(request.BarCode))
                {
                    ProductsDetails products = ProductList();
                    var product = products.products.Where(x => x.BCD == request.BarCode).FirstOrDefault();
                    if (product != null)
                    {
                        ProductDetail response = new ProductDetail()
                        {
                            BCD = product.BCD,
                            Description = product.Description,
                            Description2 = product.Description2,
                            DiscountDesc = product.DiscountDesc,
                            Price = product.Price,
                            Vat = product.Vat,
                            DiscountAmount = product.DiscountAmount,
                            FinalPrice = product.FinalPrice,
                            Weighted = product.Weighted,
                            AgeRestriction = product.AgeRestriction
                        };

                        return Ok(response);
                    }

                    return NotFound();
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Forbid();
            }
        }


        [HttpPost("AddToCart")]
        public IActionResult AddProductToCart(AddItemRequest request)
        {
            try
            {
                if (request != null && !string.IsNullOrEmpty(request.TerminalNo)
                    && !string.IsNullOrEmpty(request.StoreNo) && !string.IsNullOrEmpty(request.BarCode))
                {
                    ProductsDetails products = ProductList();
                    var product = products.products.Where(x => x.BCD == request.BarCode).FirstOrDefault();
                    if (product != null)
                    {
                        int qty = 0;
                        List<ProductDetail> productDetails = new List<ProductDetail>();
                        TransactionTotal total = new TransactionTotal();

                        if(cartProducts != null && cartProducts.Products.Count > 0)
                        {
                            productDetails.AddRange(cartProducts.Products);
                            total = cartProducts.Total;
                        }
                           
                        if(productDetails.Where(x=>x.BCD == product.BCD).Any())
                        {
                            qty = productDetails.Where(x => x.BCD == product.BCD).Select(x => x.Qty).FirstOrDefault();
                        }

                        productDetails.Add(new ProductDetail()
                        {
                            BCD = product.BCD,
                            Description = product.Description,
                            Description2 = product.Description2,
                            DiscountDesc = product.DiscountDesc,
                            Price = product.Price,
                            Vat = product.Vat,
                            DiscountAmount = product.DiscountAmount,
                            FinalPrice = product.FinalPrice,
                            Weighted = product.Weighted,
                            AgeRestriction = product.AgeRestriction,
                            Qty = qty + 1
                        });
                        total = new TransactionTotal()
                        {
                            TotalAmount = total.TotalAmount + product.FinalPrice,
                            TotalDiscount = total.TotalDiscount + product.DiscountAmount,
                            TotalItems = total.TotalItems +  1,
                            TotalVat = total.TotalVat + product.Vat,
                            TransactionId = total.TransactionId
                        };
                        cartProducts = new CartProducts()
                        {
                            Products = productDetails,
                            Total = total
                        };
                        return Ok(cartProducts);
                    }

                    return NotFound();
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Forbid();
            }
        }

        #region private methods
        private string Create16DigitString()
        {
            var builder = new StringBuilder();
            while (builder.Length < 16)
            {
                builder.Append(RNG.Next(10).ToString());
            }
            return builder.ToString();
        }
        private ProductsDetails ProductList()
        {
            try
            {
                ProductsDetails products = new ProductsDetails();
                using (StreamReader streamReader = new StreamReader(Directory.GetCurrentDirectory() + "\\Products" + "\\Products.json"))
                {
                    products = JsonConvert.DeserializeObject<ProductsDetails>(streamReader.ReadToEnd()) ?? new ProductsDetails();
                }
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new ProductsDetails();
            }
        }
        #endregion
    }
}
