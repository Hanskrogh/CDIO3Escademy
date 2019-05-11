using Escademy.Models;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using System.Globalization;
using Escademy.ViewModels;
using System.Web;
using System;
using Escademy.Dal;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Escademy.Controllers
{
    public class CoachingController : BaseController
    {
        private EscademyMDB db = new EscademyMDB();
        public ActionResult Index()
        {
            if (Session["user"] == null)
                return RedirectToAction("Login", "Auth");
            return View();
        }


        public ActionResult Edit(int aId = 0, int gId = 0)
        {
            if (aId <= 0 && gId <= 0)
                return RedirectToAction("Index");
            var cVM = new CoachingVM();

            try
            {
                var games = db.esc_games.ToList();
                var serviceTypes = new SelectList(db.esc_serviceTypes.ToList(), "Id", "Name");
                ViewBag.DBGames = games;
                ViewData["DBServiceTypes"] = serviceTypes;
                ViewBag.DBHours = PriceHourModel.GetPriceHours().ToList();

                var pGame = db.esc_profilegames.Where(x => x.gameId == gId && x.accountId == aId).FirstOrDefault();
                if (pGame == null)
                {
                    return View();
                }

                var faqs = db.esc_faq.Where(x => x.gameId == gId && x.accountId == aId).ToList();
                var pricings = db.esc_profilegamesPricing.Where(x => x.gameId == gId && x.accountId == aId).ToList();
                var files = db.esc_profilegamesFiles.Where(x => x.gameId == gId && x.accountId == aId).ToList();
                cVM = new CoachingVM()
                {
                    Title = pGame.Title,
                    Description = pGame.Description,
                    GameId = gId,
                    AccountId = aId,
                    ServiceTypeId = pGame.serviceTypeId
                };
                cVM.Faqs = faqs;
                cVM.Pricings = pricings;
                cVM.Files = files;

                return View(cVM);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        public ActionResult Edit(CoachingVM coaching, HttpPostedFileBase file)
        {
            try
            {
                if (coaching.AccountId <= 0 && coaching.GameId <= 0)
                {
                    ViewBag.Error = "Invalid model value.";
                    return View(coaching);
                }
                var games = db.esc_games.ToList();
                var serviceTypes = new SelectList(db.esc_serviceTypes.ToList(), "Id", "Name");
                ViewBag.DBGames = games;
                ViewData["DBServiceTypes"] = serviceTypes;
                ViewBag.DBHours = PriceHourModel.GetPriceHours().ToList();
                var user = GetCurrentUser();
                var pGame = db.esc_profilegames.Where(x => x.gameId == coaching.GameId && x.accountId == user.Id).FirstOrDefault();
                if (pGame == null)
                {
                    ViewBag.Error = "No game found for your reference.";
                    return View(coaching);
                }
                pGame.serviceTypeId = coaching.ServiceTypeId;
                pGame.Title = coaching.Title;
                pGame.Description = coaching.Description;

                var faqs = db.esc_faq.Where(x => x.gameId == coaching.GameId && x.accountId == user.Id).ToList();
                foreach (var item in faqs)
                {
                    db.esc_faq.Remove(item);
                }
                foreach (var item in coaching.Faqs)
                {
                    var faq = new esc_faq()
                    {
                        accountId = user.Id,
                        gameId = coaching.GameId,
                        Description = item.Description,
                        Title = item.Title
                    };
                    if (faq.Title != null && faq.Description != null)
                        db.esc_faq.Add(faq);
                }

                var pricings = db.esc_profilegamesPricing.Where(x => x.gameId == coaching.GameId && x.accountId == user.Id).ToList();
                foreach (var item in pricings)
                {
                    db.esc_profilegamesPricing.Remove(item);
                }
                foreach (var item in coaching.Pricings)
                {
                    var price = new esc_profilegamesPricing()
                    {
                        accountId = user.Id,
                        gameId = coaching.GameId,
                        Hours = item.Hours,
                        Price = item.Price
                    };
                    if (price.Hours > 0 && price.Price > 0)
                        db.esc_profilegamesPricing.Add(price);
                }
                //var files = db.esc_profilegamesFiles.Where(x => x.gameId == coaching.GameId && x.accountId == user.Id).ToList();
                //HttpPostedFileBase images1 = Request.Files["ImageData1"];
                //HttpPostedFileBase images2 = Request.Files["ImageData2"];
                //HttpPostedFileBase images3 = Request.Files["ImageData3"];
                //HttpPostedFileBase videos = Request.Files["VideoData"];
                //List<PathImageAndVideo> listOfNames = new List<PathImageAndVideo>();

                db.SaveChanges();
                ViewBag.Success = "Coaching update successfully.";
                return View(coaching);
            }
            catch (Exception ex)
            {
                return View();
            }
        }

        //public ActionResult Delete()

        [HttpGet]
        public ActionResult Create()
        {
            if (Session["user"] == null)
                return RedirectToAction("Login", "Auth");
            try
            {
                var games = db.esc_games.ToList();
                //var games = new SelectList(db.esc_games.ToList(), "Id", "Game");
                var serviceTypes = new SelectList(db.esc_serviceTypes.ToList(), "Id", "Name");

                ViewBag.DBGames = games;
                ViewData["DBServiceTypes"] = serviceTypes;
                ViewBag.DBHours = PriceHourModel.GetPriceHours().ToList();
                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        public ActionResult Create(CoachingVM model)
        {
            try
            {
                var games = db.esc_games.ToList();
                var serviceTypes = new SelectList(db.esc_serviceTypes.ToList(), "Id", "Name");
                ViewBag.DBGames = games;
                ViewData["DBServiceTypes"] = serviceTypes;
                ViewBag.DBHours = PriceHourModel.GetPriceHours().ToList();
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var user = GetCurrentUser();
                var pGame = db.esc_profilegames.Where(x => x.gameId == model.GameId && x.accountId == user.Id).FirstOrDefault();
                if (pGame != null)
                {
                    ViewBag.Error = "You are already coaching in this game! Please edit or delete that coaching and try again.";
                    return View(model);
                }
                var profileGame = new esc_profilegames()
                {
                    gameId = model.GameId,
                    accountId = user.Id,
                    Title = model.Title,
                    Description = model.Description,
                    serviceTypeId = model.ServiceTypeId
                };
                db.esc_profilegames.Add(profileGame);

                if (model.Pricings != null)
                    foreach (var item in model.Pricings)
                    {
                        var price = new esc_profilegamesPricing()
                        {
                            accountId = user.Id,
                            gameId = model.GameId,
                            Hours = item.Hours,
                            Price = item.Price
                        };
                        if (price.Hours > 0 && price.Price > 0)
                            db.esc_profilegamesPricing.Add(price);
                    }

                if (model.Faqs != null)
                    foreach (var item in model.Faqs)
                    {
                        var faq = new esc_faq()
                        {
                            accountId = user.Id,
                            gameId = model.GameId,
                            Description = item.Description,
                            Title = item.Title
                        };
                        if (faq.Title != null && faq.Description != null)
                            db.esc_faq.Add(faq);
                    }



                HttpPostedFileBase images1 = Request.Files["ImageData1"];
                HttpPostedFileBase images2 = Request.Files["ImageData2"];
                HttpPostedFileBase images3 = Request.Files["ImageData3"];
                HttpPostedFileBase videos = Request.Files["VideoData"];
                List<PathImageAndVideo> listOfNames = new List<PathImageAndVideo>();

                if (images1 != null && images1.ContentLength > 0)
                {
                    string imgName = Path.GetFileNameWithoutExtension(images1.FileName);
                    imgName = user.Id + "" + model.GameId + "" + Guid.NewGuid() + "_" + imgName;
                    imgName += Path.GetExtension(images1.FileName);
                    string _path = Path.Combine(Server.MapPath("~/CoachingImages"), imgName);
                    listOfNames.Add(new PathImageAndVideo() { Name = imgName, FileType = 1, FilePath = "CoachingImages" });
                    images1.SaveAs(_path);
                }
                if (images2.ContentLength > 0)
                {
                    string imgName = Path.GetFileNameWithoutExtension(images2.FileName);
                    imgName = user.Id + "" + model.GameId + "" + Guid.NewGuid() + "_" + imgName;
                    imgName += Path.GetExtension(images2.FileName);
                    string _path = Path.Combine(Server.MapPath("~/CoachingImages"), imgName);
                    listOfNames.Add(new PathImageAndVideo() { Name = imgName, FileType = 1, FilePath = "CoachingImages" });
                    images2.SaveAs(_path);
                }
                if (images3.ContentLength > 0)
                {
                    string imgName = Path.GetFileNameWithoutExtension(images3.FileName);
                    imgName = user.Id + "" + model.GameId + "" + Guid.NewGuid() + "_" + imgName;
                    //imgName = user.Id + "" + model.GameId + "" + DateTime.UtcNow.ToString("dd-MM-yyyy") + "_" + imgName;
                    imgName += Path.GetExtension(images3.FileName);
                    string _path = Path.Combine(Server.MapPath("~/CoachingImages"), imgName);
                    listOfNames.Add(new PathImageAndVideo() { Name = imgName, FileType = 1, FilePath = "CoachingImages" });
                    images3.SaveAs(_path);
                }
                if (videos.ContentLength > 0)
                {
                    string videoName = Path.GetFileNameWithoutExtension(videos.FileName);
                    //videoName = user.Id + "" + model.GameId + "" + DateTime.UtcNow.ToString("dd-MM-yyyy") + "_" + videoName;
                    videoName = user.Id + "" + model.GameId + "" + Guid.NewGuid() + "_" + videoName;
                    videoName += Path.GetExtension(videos.FileName);
                    string _path = Path.Combine(Server.MapPath("~/CoachingVideos"), videoName);
                    listOfNames.Add(new PathImageAndVideo() { Name = videoName, FileType = 2, FilePath = "CoachingVideos" });
                    videos.SaveAs(_path);
                }

                // Save all type of file paths into database
                foreach (var item in listOfNames)
                {
                    var file = new esc_profilegamesFiles
                    {
                        accountId = user.Id,
                        gameId = model.GameId,
                        FileName = item.Name,
                        FileType = item.FileType,
                        FilePath = item.FilePath
                    };
                    db.esc_profilegamesFiles.Add(file);
                }

                // Commit all database changes.
                db.SaveChanges();
                ViewBag.Success = "Coaching have been saved successfully!";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.Message;
                return View(model);
            }
        }

        public ActionResult Listing()
        {
            User user = null;
            if (Session["user"] != null)
            {
                user = (User)Session["user"];
            }
            return View();
        }

        public ActionResult Details(int accountId = 0, int gameId = 0)
        {
            ViewBag.accountId = accountId;
            ViewBag.gameId = gameId;


            using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT p.*, a.Created_at, a.FirstName, a.Description userdesc, a.ProfilePicture, a.Country FROM esc_profilegames p JOIN esc_accounts a ON a.Id=p.accountId WHERE accountId=@accId AND gameId=@gameId";
                    cmd.Parameters.AddWithValue("@accId", accountId);
                    cmd.Parameters.AddWithValue("@gameId", gameId);

                    var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        ViewBag.coach = new GameCoaching()
                        {
                            AccountId = accountId,
                            GameId = gameId,
                            Description = reader.GetString("Description"),
                            Title = reader.GetString("Title"),
                            SalaryUSD = reader.GetDecimal("SalaryUSD")
                        };

                        ViewBag.user = new User()
                        {
                            Country = reader.GetString("Country"),
                            Description = reader.GetString("userdesc"),
                            FirstName = reader.GetString("FirstName"),
                            Picture = reader.GetString("ProfilePicture")
                        };


                        var dt = reader.GetDateTime("Created_at");
                        ViewBag.date = $"{dt.ToString("MMM", CultureInfo.InvariantCulture)} {dt.Year}";
                    }
                }
                conn.Close();
            }

            return View();
        }

        [HttpGet]
        public JsonResult ExistGameInUserGamesByGameIdAndUserId(int _gameId)
        {
            var user = GetCurrentUser();
            var isExist = false;
            var _existUserProileGame = db.esc_profilegames.Where(x => x.accountId == user.Id && x.gameId == _gameId).FirstOrDefault();
            isExist = _existUserProileGame == null ? false : true;
            return Json(isExist, JsonRequestBehavior.AllowGet);
        }

        public class PathImageAndVideo
        {
            public string Name { get; set; }

            public int FileType { get; set; }

            public string FilePath { get; set; }
        }
        public ActionResult Dashboard()
        {
            User user = null;
            if (Session["user"] != null)
            {
                user = (User)Session["user"];
                var ActiveOrderList = new List<OrderDetailVM>();
                var SellerDetail = new List<User>();
                decimal TotalAmount = 0;
                int TotalActiveOrders = 0;

                using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
                {
                    conn.Open();
                    #region Get Active Orders-List
                    using (var cmdActiveOrder = conn.CreateCommand())
                    {
                        cmdActiveOrder.CommandText = "select g.Picture,concat(a1.FirstName,' ', a1.LastName) as BuyerName,o.mc_gross,o.date as OrderedDate from esc_orders o inner join esc_games g on o.game_id=g.Id inner join esc_accounts a1 on o.payer_account_id=a1.Id where o.receiver_id=@uid and o.order_status='Active'";
                        cmdActiveOrder.Parameters.AddWithValue("@uid", user.Id);
                        var reader = cmdActiveOrder.ExecuteReader();
                        while (reader.Read())
                        {
                            TotalActiveOrders++;
                            TotalAmount = TotalAmount + reader.GetDecimal("mc_gross");
                            ActiveOrderList.Add(new OrderDetailVM()
                            {
                                GamePicture = reader.GetString("Picture"),
                                BuyerName = reader.GetString("BuyerName"),
                                FirstLetter_BuyerName = reader.GetString("BuyerName").Substring(0, 1),
                                Price = reader.GetDecimal("mc_gross"),
                                OrderDate = String.Format("{0:ddd, MMM d, yyyy}", @reader.GetDateTime("OrderedDate"))
                            });
                        }
                        ViewBag.ActiveOrder = ActiveOrderList;
                        ViewBag.TotalAmount = TotalAmount;
                        ViewBag.TotalActiveOrders = TotalActiveOrders;
                    }
                    #endregion

                    #region Get Seller-Detail
                    using (var cmdSeller = conn.CreateCommand())
                    {
                        cmdSeller.CommandText = "select a2.FirstName as CoachFirstName,a2.LastName as CoachLastName,a2.ProfilePicture as CoachProfilePicture,a2.Level as CoachLevel from esc_accounts a2 where a2.Id=@uid";
                        cmdSeller.Parameters.AddWithValue("@uid", user.Id);
                        var reader = cmdSeller.ExecuteReader();
                        while (reader.Read())
                        {
                            ViewBag.Seller = new User()
                            {
                                FirstName = reader.GetString("CoachFirstName"),
                                LastName = reader.GetString("CoachLastName"),
                                Level = reader.GetInt32("CoachLevel"),
                                Picture = "data:image/png;base64," + reader.GetString("CoachProfilePicture")
                            };
                        }

                    }
                    #endregion
                    conn.Close();
                }

            }
            return View();
        }
        public ActionResult ManageOrders()
        {
            User user = null;
            if (Session["user"] != null)
            {
                user = (User)Session["user"];
                var NewOrderList = new List<OrderDetailVM>();
                var ActiveOrderList = new List<OrderDetailVM>();
                var DeliveredOrderList = new List<OrderDetailVM>();
                var CompletedOrderList = new List<OrderDetailVM>();
                var CancelledOrderList = new List<OrderDetailVM>();
                int NewOrderCount = 0;
                int ActiveOrderCount = 0;
                int DeliveredOrderCount = 0;
                int CompletedOrderCount = 0;
                int CancelledOrderCount = 0;
                using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
                {
                    conn.Open();
                    #region Get New Orders List
                    using (var cmdNew = conn.CreateCommand())
                    {

                        cmdNew.CommandText = "select concat(a1.FirstName,' ', a1.LastName) as BuyerName,o.payer_email as BuyerEmail,o.mc_gross,o.date as OrderedDate,o.order_status from esc_orders o inner join esc_accounts a1 on o.payer_account_id=a1.Id where o.receiver_id=@uid and o.order_status='New'";
                        cmdNew.Parameters.AddWithValue("@uid", user.Id);
                        var reader = cmdNew.ExecuteReader();
                        while (reader.Read())
                        {
                            NewOrderCount++;
                            NewOrderList.Add(new OrderDetailVM()
                            {
                                BuyerName = reader.GetString("BuyerName"),
                                BuyerEmail = reader.GetString("BuyerEmail"),
                                FirstLetter_BuyerName = reader.GetString("BuyerName").Substring(0, 1),
                                Price = reader.GetDecimal("mc_gross"),
                                OrderDate = String.Format("{0:ddd, MMM d, yyyy}", reader.GetDateTime("OrderedDate")),
                                OrderStatus = reader.GetString("order_status")
                            });
                        }
                        ViewBag.NewOrders = NewOrderList;
                        ViewBag.NewOrdersCount = NewOrderCount;
                    }
                    #endregion

                    #region Get Active Orders List
                    using (var cmdActive = conn.CreateCommand())
                    {
                        cmdActive.CommandText = "select concat(a1.FirstName,' ', a1.LastName) as BuyerName,o.payer_email as BuyerEmail,o.mc_gross,o.date as OrderedDate,o.order_status from esc_orders o inner join esc_accounts a1 on o.payer_account_id=a1.Id where o.receiver_id=@uid and o.order_status='Active'";
                        cmdActive.Parameters.AddWithValue("@uid", user.Id);
                        var reader = cmdActive.ExecuteReader();
                        while (reader.Read())
                        {
                            ActiveOrderCount++;
                            ActiveOrderList.Add(new OrderDetailVM()
                            {
                                BuyerName = reader.GetString("BuyerName"),
                                BuyerEmail = reader.GetString("BuyerEmail"),
                                FirstLetter_BuyerName = reader.GetString("BuyerName").Substring(0, 1),
                                Price = reader.GetDecimal("mc_gross"),
                                OrderDate = String.Format("{0:ddd, MMM d, yyyy}", reader.GetDateTime("OrderedDate")),
                                OrderStatus = reader.GetString("order_status")
                            });
                        }
                        ViewBag.ActiveOrders = ActiveOrderList;
                        ViewBag.ActiveOrdersCount = ActiveOrderCount;
                    }
                    #endregion

                    #region Get Delivered Orders List
                    using (var cmdDeliver = conn.CreateCommand())
                    {
                        cmdDeliver.CommandText = "select concat(a1.FirstName,' ', a1.LastName) as BuyerName,o.payer_email as BuyerEmail,o.mc_gross,o.date as OrderedDate,o.order_status from esc_orders o inner join esc_accounts a1 on o.payer_account_id=a1.Id where o.receiver_id=@uid and o.order_status='Delivered'";
                        cmdDeliver.Parameters.AddWithValue("@uid", user.Id);
                        var reader = cmdDeliver.ExecuteReader();
                        while (reader.Read())
                        {
                            DeliveredOrderCount++;
                            DeliveredOrderList.Add(new OrderDetailVM()
                            {
                                BuyerName = reader.GetString("BuyerName"),
                                BuyerEmail = reader.GetString("BuyerEmail"),
                                FirstLetter_BuyerName = reader.GetString("BuyerName").Substring(0, 1),
                                Price = reader.GetDecimal("mc_gross"),
                                OrderDate = String.Format("{0:ddd, MMM d, yyyy}", reader.GetDateTime("OrderedDate")),
                                OrderStatus = reader.GetString("order_status")
                            });
                        }
                        ViewBag.DeliveredOrders = DeliveredOrderList;
                        ViewBag.DeliveredOrdersCount = DeliveredOrderCount;
                    }
                    #endregion

                    #region Get Completed Orders List
                    using (var cmdCompleted = conn.CreateCommand())
                    {
                        cmdCompleted.CommandText = "select concat(a1.FirstName,' ', a1.LastName) as BuyerName,o.payer_email as BuyerEmail,o.mc_gross,o.date as OrderedDate,o.order_status from esc_orders o inner join esc_accounts a1 on o.payer_account_id=a1.Id where o.receiver_id=@uid and o.order_status='Completed'";
                        cmdCompleted.Parameters.AddWithValue("@uid", user.Id);
                        var reader = cmdCompleted.ExecuteReader();
                        while (reader.Read())
                        {
                            CompletedOrderCount++;
                            CompletedOrderList.Add(new OrderDetailVM()
                            {
                                BuyerName = reader.GetString("BuyerName"),
                                BuyerEmail = reader.GetString("BuyerEmail"),
                                FirstLetter_BuyerName = reader.GetString("BuyerName").Substring(0, 1),
                                Price = reader.GetDecimal("mc_gross"),
                                OrderDate = String.Format("{0:ddd, MMM d, yyyy}", reader.GetDateTime("OrderedDate")),
                                OrderStatus = reader.GetString("order_status")
                            });
                        }
                        ViewBag.CompletedOrders = CompletedOrderList;
                        ViewBag.CompletedOrdersCount = CompletedOrderCount;
                    }
                    #endregion

                    #region Get Cancelled Orders List
                    using (var cmdCancelled = conn.CreateCommand())
                    {
                        cmdCancelled.CommandText = "select concat(a1.FirstName,' ', a1.LastName) as BuyerName,o.payer_email as BuyerEmail,o.mc_gross,o.date as OrderedDate,o.order_status from esc_orders o inner join esc_accounts a1 on o.payer_account_id=a1.Id where o.receiver_id=@uid and o.order_status='Cancelled'";
                        cmdCancelled.Parameters.AddWithValue("@uid", user.Id);
                        var reader = cmdCancelled.ExecuteReader();
                        while (reader.Read())
                        {
                            CancelledOrderCount++;
                            CancelledOrderList.Add(new OrderDetailVM()
                            {
                                BuyerName = reader.GetString("BuyerName"),
                                BuyerEmail = reader.GetString("BuyerEmail"),
                                FirstLetter_BuyerName = reader.GetString("BuyerName").Substring(0, 1),
                                Price = reader.GetDecimal("mc_gross"),
                                OrderDate = String.Format("{0:ddd, MMM d, yyyy}", reader.GetDateTime("OrderedDate")),
                                OrderStatus = reader.GetString("order_status")
                            });
                        }
                        ViewBag.CancelledOrders = CancelledOrderList;
                        ViewBag.CancelledOrdersCount = CancelledOrderCount;
                    }
                    #endregion
                    conn.Close();
                }
            }
            return View();
        }

    }
}