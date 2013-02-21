using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BrightstarDB.Client;
using BrightstarDB.Samples.NerdDinner.Models;

namespace BrightstarDB.Samples.NerdDinner.Controllers
{
    public class HomeController : Controller
    {
        NerdDinnerContext _nerdDinners = new NerdDinnerContext();
        //
        // GET: /Home/

        public ActionResult Index()
        {
            var dinners = from d in _nerdDinners.Dinners select d;
            return View(dinners.ToList());
        }

        //
        // GET: /Home/Details/5

        public ActionResult Details(string id)
        {
            var dinner = _nerdDinners.Dinners.FirstOrDefault(d => d.Id.Equals(id));
            return dinner == null ? View("404") : View(dinner);
        }

        //
        // GET: /Home/Create

        public ActionResult Create()
        {
            var dinner = new Dinner {EventDate = DateTime.Now.AddDays(7)};
            return View(dinner);
        }

        //
        // POST: /Home/Create

        [HttpPost]
        public ActionResult Create(Dinner dinner)
        {
            if (ModelState.IsValid)
            {
                _nerdDinners.Dinners.Add(dinner);
                _nerdDinners.SaveChanges();
                return RedirectToAction("Index");
            }
            return View();
        }

        //
        // GET: /Home/Edit/5

        public ActionResult Edit(string id)
        {
            var dinner = _nerdDinners.Dinners.FirstOrDefault(d => d.Id.Equals(id));
            return dinner == null ? View("404") : View(dinner);
        }

        //
        // POST: /Home/Edit/5

        [HttpPost]
        public ActionResult Edit(Dinner dinner)
        {
            if (ModelState.IsValid)
            {
                dinner.Context = _nerdDinners;
                _nerdDinners.SaveChanges();
                return RedirectToAction("Index");
            }
            return View();
        }

        //
        // GET: /Home/Delete/5

        public ActionResult Delete(string id)
        {
            var dinner = _nerdDinners.Dinners.FirstOrDefault(x => x.Id.Equals(id));
            return dinner == null ? View("404") : View(dinner);
        }

        //
        // POST: /Home/Delete/5

        [HttpPost]
        public ActionResult Delete(string id, FormCollection collection)
        {
            var dinner = _nerdDinners.Dinners.FirstOrDefault(d => d.Id.Equals(id));
            if (dinner != null)
            {
                _nerdDinners.DeleteObject(dinner);
                _nerdDinners.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public ActionResult AddAttendee(string id)
        {
            var dinner = _nerdDinners.Dinners.FirstOrDefault(x => x.Id.Equals(id));
            ViewBag.Dinner = dinner;
            return dinner == null ? View("404") : View();
        }

        [HttpPost]
        public ActionResult AddAttendee(FormCollection form)
        {
            if (ModelState.IsValid)
            {
                var rsvpDinnerId = form["DinnerId"];
                var dinner = _nerdDinners.Dinners.FirstOrDefault(d => d.Id.Equals(rsvpDinnerId));
                if (dinner != null)
                {
                    var rsvp= new RSVP{AttendeeEmail = form["AttendeeEmail"], Dinner = dinner};
                    _nerdDinners.RSVPs.Add(rsvp);
                    _nerdDinners.SaveChanges();
                    return RedirectToAction("Details", new {id = rsvp.Dinner.Id});
                }
            }
            return View();
        }

        [Authorize(Roles = "editor")]
        public ViewResult SecureEditorSection()
        {
            return View();
        }

        [Authorize(Roles = "admin")]
        public ViewResult SecureAdminSection()
        {
            return View();
        }
    }
}
