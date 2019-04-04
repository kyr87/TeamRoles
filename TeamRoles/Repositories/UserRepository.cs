using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TeamRoles.Models;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity;
using System.IO;
using System.Web.Mvc;
using System.Net.Mail;
using System.Text;
using System.Data;
using System.Data.Entity;


namespace TeamRoles.Repositories
{
    public class UserRepository
    {
        private ApplicationDbContext db;
        private UserManager<ApplicationUser> _userManager;

        public UserRepository()
        {
            db = new ApplicationDbContext();
            _userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
        }

        /// <summary>
        /// Gets the Distinct number of students that follow a teacher in his courses
        /// </summary>
        /// <param name="teacher">The teacher</param>
        /// <returns>an integer representing the number of students</returns>
        public int GetTotalStudents(ApplicationUser teacher)
        {
            List<string> list = new List<string>();
            if (teacher != null)
            {
                List<Course> courses = teacher.Courses.ToList();
                foreach (var course in courses)
                {
                    foreach (var enrol in course.Enrollments.ToList())
                    {
                        list.Add(enrol.User.Id);
                    }
                }
            }
            return list.Distinct().Count();
        }

        /// <summary>
        /// Deletes all user chat messages
        /// </summary>
        /// <param name="user">the user</param>
        public void DeleteAllMessages(ApplicationUser user)
        {
            if (user != null)
            {
                List<Message> messagelist = db.Messages.Where(c => c.Receiver.Id == user.Id).ToList();
                messagelist.AddRange(db.Messages.Where(c => c.Sender.Id == user.Id).ToList());

                foreach (var message in messagelist)
                {
                    try
                    {
                        db.Messages.Remove(message);
                        db.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
            }
        }

        /// <summary>
        /// Deletes all user posts
        /// </summary>
        /// <param name="user">the user</param>
        public void DeleteAllPosts(ApplicationUser user)
        {
            if (user != null)
            {
                List<Post> postslist = db.Posts.Where(c => c.ApplicationUser.Id == user.Id).ToList();

                foreach (var post in postslist)
                {
                    try
                    {
                        db.Posts.Remove(post);
                        db.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
            }
        }

        /// <summary>
        /// Checking if birthday provided matching a students birthday
        /// </summary>
        /// <param name="Id">the student id</param>
        /// <param name="BirthDay">the birthday provided</param>
        /// <returns></returns>
        public bool CheckIfBirthDaysMatch(string Id, DateTime BirthDay)
        {
            if (Id != null && BirthDay != null)
            {
                ApplicationUser student = db.Users.Find(Id);
                if (student.BirthDay == BirthDay)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a student is a child in db
        /// </summary>
        /// <param name="student">the student to be checked</param>
        /// <returns>The child</returns>
        public Child CheckIfChildExists(ApplicationUser student)
        {
            foreach (var child in db.Children.ToList())
            {
                if (child.Childid == student.Id)
                {
                    return child;
                }
            }
            return null;
        }

        /// <summary>
        /// Accept a parent request and create connection
        /// </summary>
        /// <param name="req">the request</param>
        public void AcceptParentRequest(GenericRequest req)
        {
            ApplicationUser parent = db.Users.Find(req.User1id);
            ApplicationUser student = db.Users.Find(req.User2id);
            var child = CheckIfChildExists(student);
            if (child != null)
            {
                parent.Children.Add(child);
            }
            else
            {
                Child tobeinserted = new Child();
                tobeinserted.Childid = student.Id;
                parent.Children.Add(tobeinserted);
                db.Entry(parent).State = EntityState.Modified;
                db.Children.Add(tobeinserted);
                db.SaveChanges();
            }
            db.Requests.Remove(db.Requests.Find(req.ReqId));
            db.SaveChanges();
        }

        /// <summary>
        /// Finds all students
        /// </summary>
        /// <returns>List of students</returns>
        public List<ApplicationUser> FindAllStudent()
        {
            List<ApplicationUser> Users = db.Users.Where(u => u.Validated == true).ToList();
            List<ApplicationUser> usersInRole = new List<ApplicationUser>();

            foreach (var user in Users)
            {
                var isInRole = _userManager.IsInRole(user.Id, "Student");
                if (isInRole)
                {
                    usersInRole.Add(user);
                }
            }
            return usersInRole;
        }

        /// <summary>
        /// Finds all teachers
        /// </summary>
        /// <returns>List of teachers</returns>
        public List<ApplicationUser> FindTeachers()
        {
            List<ApplicationUser> Users = db.Users.Where(u => u.Validated == true).ToList();
            List<ApplicationUser> usersInRole = new List<ApplicationUser>();

            foreach (var user in Users)
            {
                var isInRole = _userManager.IsInRole(user.Id, "Teacher");
                if (isInRole)
                {
                    usersInRole.Add(user);
                }
            }
            return (usersInRole);
        }

        /// <summary>
        /// Deletes specific teacher
        /// </summary>
        /// <param name="pteacher">the teacher</param>
        public void DeleteTeacher(ApplicationUser pteacher)
        {
            try
            {
                ApplicationUser teacher = db.Users.Find(pteacher.Id);
                CoursesRepository repository = new CoursesRepository();
                repository.DeleteAllCourses(teacher);
                DeleteAllMessages(teacher);
                DeleteAllPosts(teacher);
                var path = teacher.Path;
                Directory.Delete(path.ToString(), true);
                db.Users.Remove(teacher);
                db.SaveChanges();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Deletes specific student
        /// </summary>
        /// <param name="pstudent">the student</param>
        public void DeleteStudent(ApplicationUser pstudent)
        {
            try
            {
                ApplicationUser student = db.Users.Find(pstudent.Id);
                CoursesRepository repository = new CoursesRepository();
                DeleteAllMessages(student);
                DeleteAllPosts(student);
                DeleteAsChildren(student);
                repository.DeleteCoursesEnrollments(student);
                db.Users.Remove(student);
                db.SaveChanges();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Delete a student's id from children table
        /// </summary>
        /// <param name="student">the student</param>
        public void DeleteAsChildren(ApplicationUser student)
        {
            Child toberemoved = db.Children.Find(student.Id);
            if (toberemoved != null)
            {
                try
                {
                    db.Children.Remove(toberemoved);
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }

        /// <summary>
        /// Find all parents
        /// </summary>
        /// <returns>list of parents</returns>
        public List<ApplicationUser> FindParents()
        {
            List<ApplicationUser> Users = db.Users.Where(u => u.Validated == true).ToList();
            List<ApplicationUser> usersInRole = new List<ApplicationUser>();

            foreach (var user in Users)
            {
                var isInRole = _userManager.IsInRole(user.Id, "Parent");
                if (isInRole)
                {
                    usersInRole.Add(user);
                }
            }
            return (usersInRole);
        }

        /// <summary>
        /// Delete specific parent
        /// </summary>
        /// <param name="pparent">the parent</param>
        public void DeleteParent(ApplicationUser pparent)
        {
            try
            {
                ApplicationUser parent = db.Users.Find(pparent.Id);
                CoursesRepository repository = new CoursesRepository();
                DeleteAllMessages(parent);
                DeleteAllPosts(parent);
                DeleteAllChildren(parent);
                db.Users.Remove(parent);
                db.SaveChanges();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Deletes all children of a parent
        /// </summary>
        /// <param name="parent">the parent</param>
        public void DeleteAllChildren(ApplicationUser parent)
        {
            List<Child> list = parent.Children.ToList();
            if (list.Count() != 0)
            {
                foreach (var child in list)
                {
                    try
                    {
                        db.Children.Remove(db.Children.Find(child.Childid));
                        db.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
            }
        }

        /// <summary>
        /// Create a parent request
        /// </summary>
        /// <param name="parent">the parent</param>
        /// <param name="student">the student</param>
        /// <returns>true if success</returns>
        public bool CreateParentRequest(ApplicationUser parent, ApplicationUser student)
        {
            ApplicationUser stud = db.Users.Find(student.Id);
            GenericRequest req = new GenericRequest();
            req.User1id = parent.Id;
            req.User2id = student.Id;
            req.Type = "ParentStudent";
            req.ApplicationUser = stud;
            stud.Requests.Add(req);
            try
            {
                db.Requests.Add(req);
                db.SaveChanges();
            }
            catch (Exception e)
            {
                throw e;
            }
            return true;
        }

        /// <summary>
        /// Accepts and erases a join request
        /// </summary>
        /// <param name="req">the request</param>
        public void AcceptJoinRequest(GenericRequest req)
        {
            GenericRequest request = db.Requests.Find(req.ReqId);
            Course course = db.Courses.Find(req.Courseid);
            ApplicationUser student = db.Users.Find(req.User2id);
            db.Courses.Attach(course);

            Enrollment enrol = new Enrollment();
            enrol.Grade = -1;
            enrol.Absences = -1;
            enrol.CourseId = course.CourseId;
            enrol.Course = course;
            enrol.UserId = student.Id;
            enrol.User = student;

            try
            {
                db.Entry(course).State = EntityState.Modified;
                db.Requests.Remove(request);
                db.Enrollments.Add(enrol);
                db.SaveChanges();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Check if there is a parentstudent or a joincourse request that matches the new one
        /// </summary>
        /// <param name="User2">the student</param>
        /// <param name="User1">the teacher or parent depending on the request type</param>
        /// <param name="Course">the course during the join request</param>
        /// <param name="type">if type of the request is parentstudent then parent is user1 </param>
        /// <returns>bool</returns>
        public bool checkIfRequestExists(ApplicationUser User2, ApplicationUser User1, Course course, string type)
        {
            if (type == "ParentStudent")
            {
                List<GenericRequest> requests = db.Requests.Where(r => r.Type == "ParentStudent").ToList();
                foreach (var req in requests)
                {
                    if (req.User2id == User2.Id && req.User1id == User1.Id)
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (type == "JoinCourse")
            {
                List<GenericRequest> requests = db.Requests.Where(r => r.Type == "JoinCourse").ToList();
                foreach (var req in requests)
                {
                    if (req.User2id == User2.Id && req.User1id == User1.Id && req.Courseid == course.CourseId)
                    {
                        return true;
                    }
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// Builds an email template to be send in case of validation
        /// </summary>
        /// <param name="bodyText">the text of the email</param>
        /// <param name="sendTo">the receiver</param>
        public static void BuildEmailTemplate(string bodyText, string sendTo)
        {
            string from, to, subject, body;
            from = "cmizikakis@gmail.com";
            to = sendTo.Trim();
            subject = "Account Validation ev-taxei";
            StringBuilder sb = new StringBuilder();
            sb.Append(bodyText);
            body = sb.ToString();
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(from);
            mail.To.Add(new MailAddress(to));
            mail.Subject = subject;
            mail.Body = body;
            mail.IsBodyHtml = true;
            SendEmail(mail);
        }

        /// <summary>
        /// Sends the email.
        /// </summary>
        /// <param name="mail">the email</param>
        public static void SendEmail(MailMessage mail)
        {
            SmtpClient client = new SmtpClient();
            client.Host = "smtp.gmail.com";
            client.Port = 587;
            client.EnableSsl = true;
            client.UseDefaultCredentials = true;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Credentials = new System.Net.NetworkCredential("entaxisys@gmail.com", "Enta3ei1235");
            client.EnableSsl = true;
            try
            {
                client.Send(mail);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Returns the average grade of a student
        /// </summary>
        /// <param name="student">the student</param>
        /// <returns>the grade in double</returns>
        public double GetAverageGrade(ApplicationUser student)
        {
            double average = 0;
            int count = 0;
            if (student != null)
            {
                List<Enrollment> list = db.Enrollments.Where(u => u.UserId == student.Id).ToList();
                foreach (var item in list)
                {
                    if (item.Grade != -1)
                    {
                        average = average + item.Grade;
                        count++;
                    }
                }
            }
            return average / count;
        }

        /// <summary>
        /// Returns the total absences of a student
        /// </summary>
        /// <param name="student">the student</param>
        /// <returns>the number of absences as int</returns>
        public int GetTotalAbsences(ApplicationUser student)
        {
            int count = 0;
            if (student != null)
            {
                List<Enrollment> list = db.Enrollments.Where(u => u.UserId == student.Id).ToList();
                foreach (var item in list)
                {
                    if (item.Absences != -1)
                    {
                        count = count + item.Absences;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Returns the age of a user based on his birthday
        /// </summary>
        /// <param name="user">the user</param>
        /// <returns>int age</returns>
        public int GetAge(ApplicationUser user)
        {
            DateTime today = DateTime.Today;
            double age = today.Subtract(user.BirthDay).TotalDays;
            age = age / 365;
            return  (int)Math.Round(age);
        }
    }
}