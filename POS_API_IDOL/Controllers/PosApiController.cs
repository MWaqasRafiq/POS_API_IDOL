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
        private static string Currency = "AED";
        public static CartProducts cartProducts;
        public PosApiController(ILogger<PosApiController> logger)
        {
                _logger = logger;
        }

        /// <summary>
        /// API will be called when the application get started to check if the terminal should be open
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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
                        cartProducts = new CartProducts();
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
                        _logger.LogWarning("Unknown request type has found. SignTerminal > SignTerminalRequest");
                        var err = new Error()
                        {
                            Code = 403,
                            Message = "Unknown request type has found.",
                            Details = "Unknown request type has found. SignTerminal > SignTerminalRequest"
                        };
                        return BadRequest(err);
                    }

                    _logger.LogInformation("SignTerminal > SignTerminalRequest OK");
                    return Ok(genericResponse);
                }
                else
                {
                    _logger.LogWarning("SignTerminal > SignTerminalRequest is null");
                    var err = new Error()
                    {
                        Code = 403,
                        Message = "No data has found.",
                        Details = "No data has found. SignTerminal > SignTerminalRequest is null"
                    };
                    return BadRequest(err);
                }
            }
            catch(Exception ex)
            {
                var err = new Error()
                {
                    Code = 500,
                    Message = "The server has thrown an exception.",
                    Details = ex.Message
                };
                _logger.LogError(ex.Message);
                return BadRequest(err);
            }
        }

        /// <summary>
        /// When customer choose to start a new transaction for checkout
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("StartTransaction")]
        public IActionResult StartTransaction(StartTransactionRequest request)
        {
            try
            {
                if (request != null && !string.IsNullOrEmpty(request.TerminalNo) && !string.IsNullOrEmpty(request.StoreNo))
                {
                    if (!string.IsNullOrEmpty(TransactionId))
                    {
                        return BadRequest("Close transaction to start new.");
                    }
                    TransactionId = Create16DigitString();
                    cartProducts = new CartProducts();
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
                    var err = new Error()
                    {
                        Code = 403,
                        Message = "No data has found.",
                        Details = "No data has found. StartTransaction > StartTransactionRequest is null"
                    };
                    return BadRequest(err);
                }
            }
            catch (Exception ex)
            {
                var err = new Error()
                {
                    Code = 500,
                    Message = "The server has thrown an exception.",
                    Details = ex.Message
                };
                _logger.LogError(ex.Message);
                return BadRequest(err);
            }
        }

        /// <summary>
        /// When customer scan an item this API will check the product details
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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
                var err = new Error()
                {
                    Code = 500,
                    Message = "The server has thrown an exception.",
                    Details = ex.Message
                };
                _logger.LogError(ex.Message);
                return BadRequest(err);
            }
        }

        /// <summary>
        /// When customer scan an item and the product exists, this API will update the transaction
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("AddToCart")]
        public IActionResult AddProductToCart(AddItemRequest request)
        {
            try
            {
                if (request != null && !string.IsNullOrEmpty(request.TerminalNo)
                    && !string.IsNullOrEmpty(request.StoreNo) && !string.IsNullOrEmpty(request.BarCode))
                {
                    if(request.TransactionId != TransactionId)
                    {
                        return Unauthorized();
                    }
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
                            productDetails.Remove(productDetails.Where(x => x.BCD == product.BCD).FirstOrDefault());
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
                            TransactionId = request.TransactionId
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
                var err = new Error()
                {
                    Code = 500,
                    Message = "The server has thrown an exception.",
                    Details = ex.Message
                };
                _logger.LogError(ex.Message);
                return BadRequest(err);
            }
        }

        /// <summary>
        /// when we need to get the total of transaction or we need to update the receipt
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("OrderTotal")]
        public IActionResult OrderTotal(StartTransactionRequest request)
        {
            try
            {
                if (request != null && !string.IsNullOrEmpty(request.TerminalNo))
                    return Ok(cartProducts);
                else
                    return BadRequest();
            }
            catch (Exception ex)
            {
                var err = new Error()
                {
                    Code = 500,
                    Message = "The server has thrown an exception.",
                    Details = ex.Message
                };
                _logger.LogError(ex.Message);
                return BadRequest(err);
            }
        }

        /// <summary>
        /// When SCO processed the payment with bank this request will log the payment info to POS
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("AddPayment")]
        public IActionResult AddPayment(AddPaymentRequest request)
        {
            try
            {
                if (request != null && !string.IsNullOrEmpty(request.TransactionId) && request.Amount > 0)
                {
                    if (request.TransactionId != TransactionId)
                    {
                        return Unauthorized();
                    }
                    return Ok(new GenericResponse()
                    {
                        Code = 1,
                        Status = "true",
                        StatusMessage = "Transaction completed successfully"
                    });
                }
                else
                    return BadRequest();
            }
            catch (Exception ex)
            {
                var err = new Error()
                {
                    Code = 500,
                    Message = "The server has thrown an exception.",
                    Details = ex.Message
                };
                _logger.LogError(ex.Message);
                return BadRequest(err);
            }
        }

        /// <summary>
        /// when the user has completed the payment and bank has responded positive this method will send the receipt for print
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("PrintReceipt")]
        public IActionResult PrintReceipt(PrintReceiptRequest request)
        {
            try
            {
                if (request != null && !string.IsNullOrEmpty(request.TransactionId))
                {
                    if (request.TransactionId != TransactionId)
                    {
                        return Unauthorized();
                    }
                    string products = string.Empty;

                    foreach (var item in cartProducts.Products)
                    {
                        products += item.Qty.ToString() + "x " + item.Description + "\t" + Currency
                                + " "+ item.FinalPrice.ToString("0.00")+" \r\n";
                        if (!string.IsNullOrEmpty(item.Description2))
                        {
                            products += "   " + item.Description2 + " \r\n";
                        }
                    }

                    //this can be empty string depending the store needs
                    string greenReceipt = "========================\r\n" +
                                        "\t  IDOL Store \r\n" +
                                        "$$PRINTLOGOxx \r\n" +
                                        "---------------------------------\r\n" +
                                        "Transaction No:\t " + TransactionId  +
                                        "\r\n Date: \t " + DateTime.Now.ToString("dd-MM-yyyy HH:mm") + 
                                        "\r\n $$PRINTBCD(code128)(4235432354543)" +
                                        "\r\n\r\n $$CUTPAPER";


                    string receipt = "========================\r\n" +
                                    "\t  IDOL Store \r\n" +
                                    "$$PRINTLOGOxx \r\n" +
                                    "========================\r\n" +
                                    "\t full of goodness\r\n" +
                                    "---------------------------------\r\n" +
                                    "Transaction No:\t " + TransactionId + "\r\n" +
                                    "Date: \t " + DateTime.Now.ToString("dd-MM-yyyy HH:mm") + " \r\n" +
                                    "---------------------------------\r\n" +
                                    "" + products + 
                                    "---------------------------------\r\n" +
                                    "VAT 5% \t\t " + cartProducts.Total.TotalVat.ToString("0.00") + " " + Currency + "\r\n" +
                                    "Total \t\t " + cartProducts.Total.TotalAmount.ToString("0.00") + " " + Currency + " " +
                                    "\r\n\r\n $$PRINTBCD(code128)(342354432354)" +
                                    "\r\n\r\n $$PRINTQR(“MEUCIQCB5EuGlXvw1LlpOGc0M1BmI+BTcpwYhcQKnzg5kXip5AIgR/ybsA7HGNwxJ+QSborSVxL3bM4dXXNqEgFx=”)" +
                                    "\r\n\r\n $$PRINTBCD(code128)(4235432354543)" +
                                    "\r\n\r\n $$CUTPAPER";

                    var payment = new PrintReceiptResponse()
                    {
                        Amount = cartProducts.Total.TotalAmount,
                        Currency = Currency,
                        DiscountAmount = cartProducts.Total.TotalDiscount,
                        Vat = cartProducts.Total.TotalVat,
                        Receipt = request.GoGreen == true ? greenReceipt : receipt
                    };
                    return Ok(payment);
                }
                else
                    return BadRequest();
            }
            catch (Exception ex)
            {
                var err = new Error()
                {
                    Code = 500,
                    Message = "The server has thrown an exception.",
                    Details = ex.Message
                };
                _logger.LogError(ex.Message);
                return BadRequest(err);
            }
        }

        /// <summary>
        /// Close the transaction to start a new one
        /// </summary>
        /// <returns></returns>
        [HttpPost("CloseTransaction")]
        public IActionResult CloseTransaction()
        {
            try
            {
                if (string.IsNullOrEmpty(TransactionId))
                {
                    return BadRequest("Transaction is already closed.");
                }
                TransactionId = string.Empty;
                return Ok("Transaction has closed.");
            }
            catch (Exception ex)
            {
                var err = new Error() 
                {
                    Code = 500,
                    Message = "The server has thrown an exception.",
                    Details = ex.Message
                };
                _logger.LogError(ex.Message);
                return BadRequest(err);
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
