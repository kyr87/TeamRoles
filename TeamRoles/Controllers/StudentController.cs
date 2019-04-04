using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.AspNet.Identity.EntityFramework;
using TeamRoles.Models;
using TeamRoles.Models.ViewModels;
using TeamRoles.Repositories;

namespace TeamRoles.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {

        private ApplicationDbContext db;
        private UserManager<ApplicationUser> _userManager;

        public StudentController()
        {
            db = new ApplicationDbContext();
            _userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
        }

        // GET: Student
        public ActionResult Index()
        {
            UserRepository repository = new UserRepository();
            return View(repository.FindAllStudent());
        }

        [Authorize(Roles = "Parent")]
        public ActionResult Student_Index_ToSelect()
        {
            UserRepository repository = new UserRepository();
            List<ApplicationUser> allstudentslist = repository.FindAllStudent();
            ApplicationUser parent = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
            List<Child> children = parent.Children.ToList();
            List<ApplicationUser> notchildrenlist = allstudentslist.ToList();
            foreach (var student in allstudentslist.ToList())
            {
                foreach(var child in children)
                {
                    if(student.Id == child.Childid)
                    {
                        notchildrenlist.Remove(student);
                    }
                }
            }
            //list.Except(childrenlist)
            return View(notchildrenlist);
        }

        [Authorize(Roles = "Parent")]
        public ActionResult Parent_Index()
        {
            ApplicationUser parent = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
            List<Child> children = parent.Children.ToList();
            List<ApplicationUser> childrenlist = new List<ApplicationUser>();
            foreach (var child in children)
            {
                childrenlist.Add(db.Users.Find(child.Childid));
            }
            return View(childrenlist);
        }

        [Authorize(Roles = "Parent")]
        public ActionResult DeleteFromChild(string id)
        {
            if(id!=null)
            {
                ApplicationUser parent = db.Users.Find(User.Identity.GetUserId());
                Child child = db.Children.Where(ch => ch.Childid == id).SingleOrDefault();
                if(child.Parent.Count()>1)
                {
                    try
                    {
                        parent.Children.Remove(child);
                        db.Entry(parent).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
                else
                {
                    db.Children.Remove(child);
                    db.SaveChanges();
                }
            }
            return RedirectToAction("Parent_Index");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Admin_Index()
        {
            UserRepository repository = new UserRepository();
            return View(repository.FindAllStudent());
        }

        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ApplicationUser student = db.Users.Find(id);
            if (student == null)
            {
                return HttpNotFound();
            }
            UserRepository repository = new UserRepository();
            StudentViewModel model = new StudentViewModel();
            model.AverageGrade = repository.GetAverageGrade(student);
            model.TotalAbsences = repository.GetTotalAbsences(student);
            model.Age = repository.GetAge(student);
            model.student = student;
            return View(model);
        }

        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ApplicationUser student = db.Users.Find(id);
            if (student == null)
            {
                return HttpNotFound();
            }
            UserRepository repository = new UserRepository();
            repository.DeleteStudent(student);     
            return RedirectToAction("Admin_Index");
        }

        public ActionResult RemoveFromCourse(string id, string coursename)
        {
            if(id!=null && coursename!=null)
            {
                ApplicationUser student = db.Users.Find(id);
                Course course = db.Courses.FirstOrDefault(c => c.CourseName == coursename);
                student.Courses.Remove(course);
                try
                {
                    db.Entry(student).State = EntityState.Modified;
                    db.SaveChanges();
                }
                catch(Exception e)
                {
                    throw e;
                }
            }
            return RedirectToAction("CourseHome", "Courses", new { id = db.Courses.FirstOrDefault(c => c.CourseName == coursename).CourseId });
        }

        public ActionResult ParentConnect(string Id,DateTime BirthDay)
        {
            ApplicationUser student = db.Users.Find(Id);
            ApplicationUser parent = db.Users.Find(User.Identity.GetUserId());
            UserRepository repository = new UserRepository();
            if(repository.CheckIfBirthDaysMatch(Id,BirthDay))
            {
               
                if(!repository.checkIfRequestExists(student,parent,null,"ParentStudent"))
                {
                    if (repository.CreateParentRequest(parent, student))
                    {
                        TempData["Request"] = "Your request has been sent";
                        return RedirectToAction("Student_Index_ToSelect", student);
                    }
                    else
                    {
                        TempData["Error"] = "Something went Wrong. Processing your request!";
                        return RedirectToAction("Student_Index_ToSelect", student);
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "You have already sent a request to this user!";
                    return RedirectToAction("Student_Index_ToSelect", student);                    
                }
            }
            else
            {
                TempData["Message"] = "Wrong BirthDate. Try again";
                return RedirectToAction("Student_Index_ToSelect", student);
            }
        }

        public ActionResult ParentNavbar()
        {
            ApplicationUser parent = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
            List<Child> children = parent.Children.ToList();
            ViewBag.messages = children.Count();
            return PartialView("_ParentNavbar");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult Error()
        {
            return View();
        }
    }
}