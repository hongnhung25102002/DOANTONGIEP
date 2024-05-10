using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using QLKS.Models;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace QLKS.Controllers
{
    public class HomeController : Controller
    {
        private dataQLKSEntities db = new dataQLKSEntities();
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        [HttpPost]
        public ActionResult Contact(String ho_ten,String mail,String noi_dung)
        {
            if (noi_dung == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            tblKhachHang kh = (tblKhachHang)Session["KH"];
            if (kh == null)
            {
                if (ho_ten == null || mail == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (noi_dung.Length >= 500)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            tblTinNhan tn = new tblTinNhan();
            if (kh == null)
            {
                tn.ho_ten = ho_ten;
                tn.mail = mail;
            }
            else
            {
                tn.ma_kh = kh.ma_kh;
            }
            tn.noi_dung = noi_dung;
            tn.ngay_gui = DateTime.Now;
            try
            {
                db.tblTinNhans.Add(tn);
                db.SaveChanges();
                ModelState.AddModelError("", "Gửi ticket thành công !");
            }
            catch
            {
                ModelState.AddModelError("", "Có lỗi xảy ra!");
            }
            return View();
        }

        [HttpGet]
        public ActionResult FindRoom()
        {
            return RedirectToAction("Index", "Home");
        }
        [HttpPost]
        public ActionResult FindRoom(String datestart, String dateend, int? soluongnguoi)
        {
            List<tblPhong> li = new List<tblPhong>();
            if (datestart.Equals("") || dateend.Equals(""))
            {
                li = db.tblPhongs.ToList();
            }
            else
            {
                Session["ds_ma_phong"] = null;
                Session["ngay_vao"] = datestart;
                Session["ngay_ra"] = dateend;
                if (soluongnguoi.HasValue && soluongnguoi .Value > 0)
                    Session["so_luong_nguoi"] = soluongnguoi;
              
                datestart = DateTime.ParseExact(datestart, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyy/MM/dd");
                dateend = DateTime.ParseExact(dateend, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyy/MM/dd");

                DateTime dateS = (DateTime.Parse(datestart)).AddHours(12);
                DateTime dateE = (DateTime.Parse(dateend)).AddHours(12);
                if (soluongnguoi.HasValue && soluongnguoi.Value > 0)
                {
                    li = db.tblPhongs.Where(t => !(db.tblPhieuDatPhongs.Where(m => (m.ma_tinh_trang == 1 || m.ma_tinh_trang == 2)
                    && m.ngay_ra > dateS && m.ngay_vao < dateE))
                    .Select(m => m.ma_phong).ToList().Contains(t.ma_phong) && t.tblLoaiPhong.so_luong_nguoi == soluongnguoi).ToList();
                }
                else
                {
                    li = db.tblPhongs.Where(t => !(db.tblPhieuDatPhongs.Where(m => (m.ma_tinh_trang == 1 || m.ma_tinh_trang == 2)
                    && m.ngay_ra > dateS && m.ngay_vao < dateE))
                    .Select(m => m.ma_phong).ToList().Contains(t.ma_phong)).ToList();
                }
                
            }
            return View(li);
        }
        public ActionResult ChonPhong(string id)
        {
            //Session["ma_phong"] = id;
            try
            {
                List<int> ds;
                ds = (List<int>)Session["ds_ma_phong"];
                if (ds == null)
                    ds = new List<int>();
                ds.Add(Int32.Parse(id));
                Session["ds_ma_phong"] = ds;
                ViewBag.result = "success";
            }
            catch
            {
                ViewBag.result = "error";
            }
            return View();
            //return RedirectToAction("BookRoom", "Home");
        }
        public ActionResult HuyChon(string id)
        {
            try
            {
                List<int> ds;
                ds = (List<int>)Session["ds_ma_phong"];
                if (ds == null)
                    ds = new List<int>();
                ds.Remove(Int32.Parse(id));
                Session["ds_ma_phong"] = ds;
                ViewBag.result = "success";
            }
            catch
            {
                ViewBag.result = "error";
            }
            return View();
        }
        public ActionResult BookRoom()
        {
            if (Session["KH"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            AutoHuyPhieuDatPhong();
            tblKhachHang kh = (tblKhachHang)Session["KH"];
            ViewBag.ma_kh = kh.ma_kh;
            ViewBag.ten_kh = kh.ho_ten;
            ViewBag.ngay_dat = DateTime.Now;
            ViewBag.ngay_vao = (String)Session["ngay_vao"];
            ViewBag.ngay_ra = (String)Session["ngay_ra"];

            //if (Session["ma_phong"] != null)
            //{
            //    ViewBag.ma_phong = (String)Session["ma_phong"];
            //    int map = Int32.Parse((String)Session["ma_phong"]);
            //    tblPhong p = (tblPhong)db.tblPhongs.Find(map);
            //    ViewBag.so_phong = p.so_phong;
            //}
            String sp = "";
            List<int> ds;
            ds = (List<int>)Session["ds_ma_phong"];
            if (ds == null)
                ds = new List<int>();
            ViewBag.ma_phong = JsonConvert.SerializeObject(ds);
            decimal totalTien = 0;
            foreach (var item in ds)
            {
                tblPhong p = (tblPhong)db.tblPhongs.Find(Int32.Parse(item.ToString()));
                sp += p.so_phong.ToString() + ", ";
                totalTien += (decimal)p.tblLoaiPhong.gia.Value;
            }
            ViewBag.tong_tien = totalTien > 0 ?totalTien.ToString("#,###"):"";
            ViewBag.so_phong = sp;
            var liP = db.tblPhieuDatPhongs.Where(u => u.ma_kh == kh.ma_kh && u.ma_tinh_trang == 1).ToList();
            return View(liP);
        }
        private void AutoHuyPhieuDatPhong()
        {
            var datenow = DateTime.Now;
            var tblPhieuDatPhongs = db.tblPhieuDatPhongs.Where(u => u.ma_tinh_trang == 1).Include(t => t.tblKhachHang).Include(t => t.tblPhong).Include(t => t.tblTinhTrangPhieuDatPhong).ToList();
            foreach (var item in tblPhieuDatPhongs)
            {
                System.Diagnostics.Debug.WriteLine((item.ngay_vao - datenow).Value.Days);
                if ((item.ngay_vao - datenow).Value.Days < 0)
                {
                    item.ma_tinh_trang = 3;
                    db.Entry(item).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
        }
        public ActionResult Result(String ma_kh, String ngay_vao, String ngay_ra, String ma_phong,String don_vi_tinh, int payment_type, decimal total_payment)
        {
            if (ma_kh == null || ngay_vao == null || ngay_ra == null || ma_phong == null)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                tblPhieuDatPhong tgd = new tblPhieuDatPhong();
                List<int> ds = JsonConvert.DeserializeObject<List<int>>(ma_phong);
                tgd.ma_kh = ma_kh;
                tgd.ma_tinh_trang = 1;
                tgd.ngay_dat = DateTime.Now;
                tgd.ngay_vao = (DateTime.ParseExact(ngay_vao, "dd/MM/yyyy", CultureInfo.InvariantCulture)).AddHours(12);
                tgd.ngay_ra = (DateTime.ParseExact(ngay_ra, "dd/MM/yyyy", CultureInfo.InvariantCulture)).AddHours(12);
                tgd.don_vi_tinh = don_vi_tinh;
                tgd.payment_type = payment_type;
                tgd.total_payment = total_payment;
                //tgd.ma_nhan_phong = ma_kh + ngay_vao.Replace("/","");
                ViewBag.MaCode = "";
                try
                {
                    for (int i = 0; i < ds.Count; i++)
                    {
                        tgd.ma_phong = ds[i];
                        var phong = db.tblPhongs.Where(p => p.ma_phong == tgd.ma_phong).FirstOrDefault();
                        if (phong != null && phong.ma_tang != null)
                        {
                            if (phong.tblTang.ten_tang.Trim().ToLower() == "Tầng 1".Trim().ToLower())
                            {
                                tgd.ma_nhan_phong = "A/a" + phong.so_phong + ma_kh + ngay_vao.Replace("/", "");
                            }
                            else if (phong.tblTang.ten_tang.Trim().ToLower() == "Tầng 2".Trim().ToLower())
                            {
                                tgd.ma_nhan_phong = "B/b" + phong.so_phong + ma_kh + ngay_vao.Replace("/", "");
                            }
                            else if (phong.tblTang.ten_tang.Trim().ToLower() == "Tầng 3".Trim().ToLower())
                            {
                                tgd.ma_nhan_phong = "C/c" + phong.so_phong + ma_kh + ngay_vao.Replace("/", "");
                            }
                        }
                        db.tblPhieuDatPhongs.Add(tgd);
                        db.SaveChanges();
                        ViewBag.Result = "success";
                        ViewBag.MaCode = tgd.ma_nhan_phong;
                    }
                    ViewBag.ngay_vao = tgd.ngay_vao;
                    setNull();
                }
                catch
                {
                    ViewBag.Result = "error";
                }
            }
            return View();
        }

        public ActionResult HuyPhieuDatPhong()
        {
            setNull();
            return RedirectToAction("BookRoom", "Home");
        }
        private void setNull()
        {
            Session["ngay_vao"] = null;
            Session["ngay_ra"] = null;
            Session["ma_phong"] = null;
            Session["ds_ma_phong"] = null;
            Session["so_luong_nguoi"] = null;
        }
        public ActionResult Chat()
        {
            return View();
        }
        public ActionResult Upload()
        {
            return View();
        }
        public ActionResult Slider(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            //tblPhong p = db.tblPhongs.Include(a => a.tblLoaiPhong).Where(a=>a.ma_phong==id).First();
            tblLoaiPhong lp = db.tblLoaiPhongs.Find(id);
            return View(lp);
        }
        public ActionResult SMS(String ho_ten,String mail,String noi_dung)
        {
            if(noi_dung == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            tblKhachHang kh = (tblKhachHang)Session["KH"];
            if(kh == null)
            {
                if(ho_ten == null || mail == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if(noi_dung.Length >= 500)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            tblTinNhan tn = new tblTinNhan();
            if (kh == null)
            {
                tn.ho_ten = ho_ten;
                tn.mail = mail;
            }
            else
            {
                tn.ma_kh = kh.ma_kh;
            }
            tn.noi_dung = noi_dung;
            try
            {
                db.tblTinNhans.Add(tn);
                db.SaveChanges();
                ViewBag.result = "success";
            }
            catch
            {
                ViewBag.result = "error";
            }
            return View();
        }
    }
}