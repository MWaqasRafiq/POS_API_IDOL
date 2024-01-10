using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using POS_API_IDOL.Model;
using Serilog;
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
        public static CartProducts lastCartProducts;

        /// <summary>
        /// Initialize constructor
        /// </summary>
        /// <param name="logger"></param>
        public PosApiController(ILogger<PosApiController> logger)
        {
            _logger = logger;
        }


        /// <summary>
        /// Sign the terminal on the start of application
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("SignTerminal")]
        [ProducesResponseType(typeof(GenericResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Error), (int)HttpStatusCode.BadRequest)]
        public IActionResult SignTerminal(SignTerminalRequest request)
        {
            var err = new Error();
            try
            {
                _logger.LogInformation("Request Log:: " + JsonConvert.SerializeObject(request));
                if (request != null && !string.IsNullOrEmpty(request.Type))
                {
                    GenericResponse genericResponse = new GenericResponse();
                    if (request.Type.ToLower() == "on")
                    {
                        genericResponse.Message = "Terminal signed on successfully";
                        genericResponse.Code = 1;
                    }
                    else if (request.Type.ToLower() == "off")
                    {
                        cartProducts = new CartProducts();
                        genericResponse.Message = "Terminal signed off successfully";
                        genericResponse.Code = 0;
                    }
                    else
                    {
                        err = new Error()
                        {
                            Code = 403,
                            Message = "Unknown request type has found.",
                            Details = "Unknown request type has found. SignTerminal > SignTerminalRequest"
                        };
                        _logger.LogError("Unknown request type has found. SignTerminal > SignTerminalRequest");
                        return BadRequest(err);
                    }

                    return Ok(genericResponse);
                }
                else
                {
                    err = new Error()
                    {
                        Code = 404,
                        Message = "No data has found.",
                        Details = "No data has found. SignTerminal > SignTerminalRequest is null"
                    };
                    _logger.LogError("No data has found. SignTerminal > SignTerminalRequest is null");
                    return BadRequest(err);
                }
            }
            catch (Exception ex)
            {
                err = new Error()
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
        [ProducesResponseType(typeof(StartTransactionResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Error), (int)HttpStatusCode.BadRequest)]
        public IActionResult StartTransaction(StartTransactionRequest request)
        {
            var err = new Error();
            try
            {
                _logger.LogInformation("Request Log:: " + JsonConvert.SerializeObject(request));
                if (request != null && !string.IsNullOrEmpty(request.TerminalNo) && !string.IsNullOrEmpty(request.StoreNo))
                {
                    if (!string.IsNullOrEmpty(TransactionId))
                    {
                        TransactionId = string.Empty;
                        lastCartProducts = cartProducts;
                        cartProducts = new CartProducts();
                    }
                    TransactionId = Create16DigitString();
                    cartProducts = new CartProducts();
                    StartTransactionResponse response = new StartTransactionResponse()
                    {
                        TransactionId = TransactionId,
                        Message = "New transaction started successfully"
                    };

                    return Ok(response);
                }
                else
                {
                    err = new Error()
                    {
                        Code = 404,
                        Message = "No data has found.",
                        Details = "No data has found. StartTransaction > StartTransactionRequest is null"
                    };
                    _logger.LogError("No data has found. StartTransaction > StartTransactionRequest is null");
                    return BadRequest(err);
                }
            }
            catch (Exception ex)
            {
                err = new Error()
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
        [ProducesResponseType(typeof(ProductDetail), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Error), (int)HttpStatusCode.BadRequest)]
        public IActionResult CheckProductDetails(CheckProductRequest request)
        {
            var err = new Error();
            try
            {
                _logger.LogInformation("Request Log:: " + JsonConvert.SerializeObject(request));
                if (request != null && !string.IsNullOrEmpty(request.TerminalNo)
                    && !string.IsNullOrEmpty(request.StoreNo) && !string.IsNullOrEmpty(request.BarCode))
                {
                    ProductsDetails products = ProductList();
                    var product = products.products.Where(x => x.BarCode == request.BarCode).FirstOrDefault();
                    if (product != null)
                    {
                        ProductDetail response = new ProductDetail()
                        {
                            BarCode = product.BarCode,
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
                    err = new Error()
                    {
                        Code = 403,
                        Message = "Unkown product has scanned.",
                        Details = "Unkown product has scanned  -  " + request.BarCode
                    };
                    _logger.LogError("Unkown product has scanned  -  " + request.BarCode);
                    return NotFound(err);
                }
                else
                {
                    err = new Error()
                    {
                        Code = 404,
                        Message = "No data has found.",
                        Details = "No data has found. ProductDetails > CheckProductRequest"
                    };
                    _logger.LogError("No data has found. ProductDetails > CheckProductRequest");
                    return BadRequest(err);
                }
            }
            catch (Exception ex)
            {
                err = new Error()
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
        /// When customer scan an item and system updates the transaction
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("AddToCart")]
        [ProducesResponseType(typeof(CartProducts), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Error), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(Error), (int)HttpStatusCode.Unauthorized)]
        public IActionResult AddProductToCart(AddItemRequest request)
        {
            var err = new Error();
            try
            {
                _logger.LogInformation("Request Log:: " + JsonConvert.SerializeObject(request));
                if (request != null && !string.IsNullOrEmpty(request.TransactionId) && !string.IsNullOrEmpty(request.BarCode))
                {
                    if (request.TransactionId != TransactionId)
                    {
                        err = new Error()
                        {
                            Code = 403,
                            Message = "Unkown transaction",
                            Details = "Unkown transaction  -  TransactionId: " + request.TransactionId + "  -  BarCode: " + request.BarCode
                        };
                        _logger.LogError("Unkown transaction  -  TransactionId: " + request.TransactionId + "  -  BarCode: " + request.BarCode);
                        return Unauthorized(err);
                    }
                    ProductsDetails products = ProductList();
                    var product = products.products.Where(x => x.BarCode == request.BarCode).FirstOrDefault();
                    if (product != null)
                    {
                        int qty = 0;
                        List<ProductDetail> productDetails = new List<ProductDetail>();
                        TransactionTotal total = new TransactionTotal();

                        if (cartProducts != null && cartProducts.Products.Count > 0)
                        {
                            productDetails.AddRange(cartProducts.Products);
                            total = cartProducts.Total;
                        }

                        if (productDetails.Where(x => x.BarCode == product.BarCode).Any())
                        {
                            qty = productDetails.Where(x => x.BarCode == product.BarCode).Select(x => x.Qty).FirstOrDefault();
                            productDetails.Remove(productDetails.Where(x => x.BarCode == product.BarCode).FirstOrDefault());
                        }

                        productDetails.Add(new ProductDetail()
                        {
                            BarCode = product.BarCode,
                            Description = product.Description,
                            Description2 = product.Description2,
                            DiscountDesc = product.DiscountDesc,
                            Price = product.Price,
                            Vat = product.Vat,
                            DiscountAmount = product.DiscountAmount,
                            FinalPrice = product.FinalPrice,
                            Weighted = product.Weighted,
                            AgeRestriction = product.AgeRestriction,
                            Qty = qty + request.Qty
                        });
                        total = new TransactionTotal()
                        {
                            TotalAmount = total.TotalAmount + product.FinalPrice,
                            TotalDiscount = total.TotalDiscount + product.DiscountAmount,
                            TotalItems = total.TotalItems + 1,
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
                    err = new Error()
                    {
                        Code = 404,
                        Message = "No data has found.",
                        Details = "No data has found. AddProductToCart > AddItemRequest"
                    };
                    _logger.LogError("No data has found. AddProductToCart > AddItemRequest");
                    return BadRequest(err);
                }
                else
                {
                    err = new Error()
                    {
                        Code = 404,
                        Message = "No data has found.",
                        Details = "No data has found. AddProductToCart > AddItemRequest"
                    };
                    _logger.LogError("No data has found. AddProductToCart > AddItemRequest");
                    return BadRequest(err);
                }
            }
            catch (Exception ex)
            {
                err = new Error()
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
        /// When customer scan an item to void, this API will update the transaction
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("VoidFromCart")]
        [ProducesResponseType(typeof(CartProducts), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Error), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(Error), (int)HttpStatusCode.Unauthorized)]
        public IActionResult VoidProductFromCart(VoidItemRequest request)
        {
            var err = new Error();
            try
            {
                _logger.LogInformation("Request Log:: " + JsonConvert.SerializeObject(request));
                if (request != null && !string.IsNullOrEmpty(request.TransactionId) && !string.IsNullOrEmpty(request.BarCode))
                {
                    if (request.TransactionId != TransactionId)
                    {
                        err = new Error()
                        {
                            Code = 403,
                            Message = "Unkown transaction",
                            Details = "Unkown transaction  -  TransactionId: " + request.TransactionId + "  -  BarCode: " + request.BarCode
                        };
                        _logger.LogError("Unkown transaction  -  TransactionId: " + request.TransactionId + "  -  BarCode: " + request.BarCode);
                        return Unauthorized(err);
                    }
                    ProductsDetails products = ProductList();
                    var product = products.products.Where(x => x.BarCode == request.BarCode).FirstOrDefault();
                    if (product != null)
                    {
                        int qty = 0;
                        List<ProductDetail> productDetails = new List<ProductDetail>();
                        TransactionTotal total = new TransactionTotal();

                        productDetails.AddRange(cartProducts.Products);
                        total = cartProducts.Total;

                        if (productDetails.Where(x => x.BarCode == product.BarCode).Any())
                        {
                            productDetails.Remove(productDetails.Where(x => x.BarCode == product.BarCode).FirstOrDefault());
                        }

                        total = new TransactionTotal()
                        {
                            TotalAmount = productDetails.Sum(x => x.FinalPrice),
                            TotalDiscount = productDetails.Sum(x => x.DiscountAmount),
                            TotalItems = productDetails.Sum(x=>x.Qty),
                            TotalVat = productDetails.Sum(x => x.Vat),
                            TransactionId = request.TransactionId
                        };
                        cartProducts = new CartProducts()
                        {
                            Products = productDetails,
                            Total = total
                        };
                        return Ok(cartProducts);
                    }
                    err = new Error()
                    {
                        Code = 404,
                        Message = "No data has found.",
                        Details = "No data has found. VoidProductFromCart > VoidItemRequest"
                    };
                    _logger.LogError("No data has found. VoidProductFromCart > VoidItemRequest");
                    return BadRequest(err);
                }
                else
                {
                    err = new Error()
                    {
                        Code = 404,
                        Message = "No data has found.",
                        Details = "No data has found prams. VoidProductFromCart > VoidItemRequest"
                    };
                    _logger.LogError("No data has found prams. VoidProductFromCart > VoidItemRequest");
                    return BadRequest(err);
                }
            }
            catch (Exception ex)
            {
                err = new Error()
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
        /// Get the total of transaction and to update the receipt
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("OrderTotal")]
        [ProducesResponseType(typeof(CartProducts), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Error), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(Error), (int)HttpStatusCode.Unauthorized)]
        public IActionResult OrderTotal(OrderTotalRequest request)
        {
            var err = new Error();
            try
            {
                _logger.LogInformation("Request Log:: " + JsonConvert.SerializeObject(request));
                if (request != null && !string.IsNullOrEmpty(request.TransactionId))
                {
                    if (request.TransactionId != TransactionId)
                    {
                        err = new Error()
                        {
                            Code = 403,
                            Message = "Unkown transaction",
                            Details = "Unkown transaction." 
                        };
                        _logger.LogError("Unkown transaction.");
                        return Unauthorized(err);
                    }
                    return Ok(cartProducts);
                }
                else
                {
                    err = new Error()
                    {
                        Code = 403,
                        Message = "Unkown request",
                        Details = "Unkown request. OrderTotal > StartTransactionRequest"
                    };
                    _logger.LogError("Unkown request. OrderTotal > StartTransactionRequest - Terminal:");
                    return BadRequest(err);
                }
            }
            catch (Exception ex)
            {
                err = new Error()
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
        [ProducesResponseType(typeof(AddPaymentResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Error), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(Error), (int)HttpStatusCode.Unauthorized)]
        public IActionResult AddPayment(AddPaymentRequest request)
        {
            var err = new Error();
            try
            {
                _logger.LogInformation("Request Log:: " + JsonConvert.SerializeObject(request));
                if (request != null && !string.IsNullOrEmpty(request.TransactionId) && request.Amount > 0)
                {
                    if (request.TransactionId != TransactionId)
                    {
                        err = new Error()
                        {
                            Code = 403,
                            Message = "Unkown transaction",
                            Details = "Unkown transaction  -  TransactionId: " + request.TransactionId
                        };
                        _logger.LogError("Unkown transaction  -  TransactionId: " + request.TransactionId);
                        return Unauthorized(err);
                    }
                    return Ok(new AddPaymentResponse()
                    {
                        Code = 1,
                        Message = "Transaction completed successfully",
                    });
                }
                else
                {
                    err = new Error()
                    {
                        Code = 404,
                        Message = "No data has found.",
                        Details = "No data has found. AddPayment > AddPaymentRequest"
                    };
                    _logger.LogError("No data has found. AddPayment > AddPaymentRequest");
                    return BadRequest(err);
                }
            }
            catch (Exception ex)
            {
                err = new Error()
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
        /// Receipt for printing 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("PrintReceipt")]
        [ProducesResponseType(typeof(PrintReceiptResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Error), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(Error), (int)HttpStatusCode.Unauthorized)]
        public IActionResult PrintReceipt(PrintReceiptRequest request)
        {
            var err = new Error();
            try
            {
                _logger.LogInformation("Request Log:: " + JsonConvert.SerializeObject(request));
                if (request != null && !string.IsNullOrEmpty(request.TransactionId))
                {
                    if (request.TransactionId != TransactionId)
                    {
                        err = new Error()
                        {
                            Code = 403,
                            Message = "Unkown transaction",
                            Details = "Unkown transaction  -  TransactionId: " + request.TransactionId
                        };
                        _logger.LogError("Unkown transaction  -  TransactionId: " + request.TransactionId);
                        return Unauthorized(err);
                    }
                    string products = string.Empty;
                    string receipt = string.Empty;

                    if (request.GoGreen == true)
                    {
                        //this can be empty string depending the store needs
                        receipt = "========================\r\n " +
                        "\t   IDOL Store \r\n " +
                        "$$PRINTLOGOxx \r\n " +
                        "---------------------------------\r\n " +
                        "Transaction No:\t  " + TransactionId +
                        "\r\n  Date: \t  " + DateTime.Now.ToString("dd-MM-yyyy HH:mm") +
                        "\r\n  $$PRINTBCD(108)(4235432354543)" +
                        "\r\n \r\n  $$CUTPAPER";
                    }
                    else
                    {
                        foreach (var item in cartProducts.Products)
                        {
                            products += item.Qty.ToString() + "x " + item.Description + "\t " + Currency
                                    + " " + item.FinalPrice.ToString("0.00") + " \r\n ";
                            if (!string.IsNullOrEmpty(item.Description2))
                            {
                                products += "   " + item.Description2 + " \r\n ";
                            }
                        }

                        receipt = "========================\r\n " +
                        "\t   IDOL Store \r\n " +
                        "$$PRINTLOGO22 \r\n " +
                        "========================\r\n " +
                        "\t  full of goodness\r\n " +
                        "---------------------------------\r\n " +
                        "Transaction No:\t  " + TransactionId + "\r\n " +
                        "Date: \t  " + DateTime.Now.ToString("dd-MM-yyyy HH:mm") + " \r\n " +
                        "---------------------------------\r\n " +
                        "" + products +
                        "---------------------------------\r\n " +
                        "VAT 5% \t \t  " + cartProducts.Total.TotalVat.ToString("0.00") + " " + Currency + "\r\n " +
                        "Total \t \t  " + cartProducts.Total.TotalAmount.ToString("0.00") + " " + Currency + " " +
                        "\r\n \r\n  $$PRINTBCD(108)(8965412356231)" +
                        "\r\n \r\n  $$PRINTQR(MEUCIQCB5EuGlXvw1LlpOGc0M1BmI+BTcpwYhcQKnzg5kXip5AIgR/ybsA7HGNwxJ+QSborSVxL3bM4dXXNqEgFx=)" +
                        "\r\n \r\n  $$CUTPAPER";
                    }

                    var payment = new PrintReceiptResponse()
                    {
                        Amount = cartProducts.Total.TotalAmount,
                        Currency = Currency,
                        DiscountAmount = cartProducts.Total.TotalDiscount,
                        Vat = cartProducts.Total.TotalVat,
                        Receipt = receipt
                    };
                    return Ok(payment);
                }
                else
                {
                    err = new Error()
                    {
                        Code = 404,
                        Message = "No data has found.",
                        Details = "No data has found. PrintReceipt > PrintReceiptRequest"
                    };
                    _logger.LogError("No data has found. PrintReceipt > PrintReceiptRequest");
                    return BadRequest(err);
                }
            }
            catch (Exception ex)
            {
                err = new Error()
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
        /// To print last transactional receipt
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("PrintLastReceipt")]
        [ProducesResponseType(typeof(PrintReceiptResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Error), (int)HttpStatusCode.BadRequest)]
        public IActionResult PrintLastReceipt(PrintLastReceiptRequest request)
        {
            var err = new Error();
            try
            {
                _logger.LogInformation("Request Log:: " + JsonConvert.SerializeObject(request));
                if (request != null && !string.IsNullOrEmpty(request.StoreNo) && !string.IsNullOrEmpty(request.TerminalNo))
                {
                    if (lastCartProducts == null || lastCartProducts.Products.Count() == 0)
                    {
                        err = new Error()
                        {
                            Code = 404,
                            Message = "No data has found.",
                            Details = "No data has found. PrintLastReceipt > PrintReceiptRequest > lastCartProducts"
                        };
                        _logger.LogError("No data has found. PrintLastReceipt > PrintReceiptRequest > lastCartProducts");
                        return BadRequest(err);
                    }
                    string products = string.Empty;

                    foreach (var item in lastCartProducts.Products)
                    {
                        products += item.Qty.ToString() + "x " + item.Description + "\t " + Currency
                                + " " + item.FinalPrice.ToString("0.00") + " \r\n ";
                        if (!string.IsNullOrEmpty(item.Description2))
                        {
                            products += "   " + item.Description2 + " \r\n ";
                        }
                    }

                    string receipt = "========================\r\n " +
                                    "\t   IDOL Store \r\n " +
                                    "$$PRINTLOGOxx \r\n " +
                                    "========================\r\n " +
                                    "\t  full of goodness\r\n " +
                                    "---------------------------------\r\n " +
                                    "Transaction No:\t  " + lastCartProducts.Total.TransactionId + "\r\n " +
                                    "Date: \t  " + DateTime.Now.ToString("dd-MM-yyyy HH:mm") + " \r\n " +
                                    "---------------------------------\r\n " +
                                    "" + products +
                                    "---------------------------------\r\n " +
                                    "VAT 5% \t \t  " + lastCartProducts.Total.TotalVat.ToString("0.00") + " " + Currency + "\r\n " +
                                    "Total \t \t  " + lastCartProducts.Total.TotalAmount.ToString("0.00") + " " + Currency + " " +
                                    "\r\n \r\n  $$PRINTBCD(code128)(342354432354)" +
                                    "\r\n \r\n  $$PRINTQR(“MEUCIQCB5EuGlXvw1LlpOGc0M1BmI+BTcpwYhcQKnzg5kXip5AIgR/ybsA7HGNwxJ+QSborSVxL3bM4dXXNqEgFx=”)" +
                                    "\r\n \r\n  $$PRINTBCD(code128)(4235432354543)" +
                                    "\r\n \r\n  $$CUTPAPER";

                    var payment = new PrintReceiptResponse()
                    {
                        Amount = lastCartProducts.Total.TotalAmount,
                        Currency = Currency,
                        DiscountAmount = lastCartProducts.Total.TotalDiscount,
                        Vat = lastCartProducts.Total.TotalVat,
                        Receipt = receipt
                    };
                    return Ok(payment);
                }
                else
                {
                    err = new Error()
                    {
                        Code = 404,
                        Message = "No data has found.",
                        Details = "No data has found. PrintLastReceipt > PrintReceiptRequest"
                    };
                    _logger.LogError("No data has found. PrintLastReceipt > PrintReceiptRequest");
                    return BadRequest(err);
                }
            }
            catch (Exception ex)
            {
                err = new Error()
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
        /// Close the current transaction
        /// </summary>
        /// <returns></returns>
        [HttpPost("CloseTransaction")]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Error), (int)HttpStatusCode.BadRequest)]
        public IActionResult CloseTransaction()
        {
            var err = new Error();
            try
            {
                _logger.LogInformation("Request Log:: CloseTransaction");
                if (string.IsNullOrEmpty(TransactionId))
                {
                    err = new Error()
                    {
                        Code = 400,
                        Message = "Transaction is already closed.",
                        Details = "Transaction is already closed. CloseTransaction"
                    };
                    _logger.LogError("Transaction is already closed. CloseTransaction");
                    return BadRequest(err);
                }
                TransactionId = string.Empty;
                lastCartProducts = cartProducts;
                cartProducts = new CartProducts();

                return Ok(new AddPaymentResponse()
                {
                    Code = 1,
                    Message = "Transaction has closed.",
                });
            }
            catch (Exception ex)
            {
                err = new Error()
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
