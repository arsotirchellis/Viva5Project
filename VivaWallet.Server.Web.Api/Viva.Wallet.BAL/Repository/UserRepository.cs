﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Viva.Wallet.BAL.Helpers;
using Viva.Wallet.BAL.Models;
using VivaWallet.DAL;

namespace Viva.Wallet.BAL.Repository
{
    public class UserRepository : IDisposable
    {
        protected UnitOfWork uow;

        public UserRepository()
        {
            uow = new UnitOfWork();
        }

        // OK
        public IList<UserModel> GetAllUsers()
        {
            return uow.UserRepository.All()?
                    .Select(e => new UserModel()
                    {
                        Id = e.Id,
                        Username = e.Username,
                        IsVerified = e.IsVerified,
                        VerificationToken = e.VerificationToken,
                        CreatedDateTime = e.CreatedDateTime,
                        UpdatedDateTime = e.UpdateDateTime,
                        Name = e.Name,
                        ShortBio = e.ShortBio,
                        AvatarImage = e.AvatarImage
                    }).OrderByDescending(e => e.UpdatedDateTime).ToList();
        }

        // OK
        public IList<UserModel> GetLastTenRegisteredUsers()
        {
            
            return uow.UserRepository.All()?
                    .Select(e => new UserModel()
                    {
                        Id = e.Id,
                        Username = e.Username,
                        IsVerified = e.IsVerified,
                        VerificationToken = e.VerificationToken,
                        CreatedDateTime = e.CreatedDateTime,
                        UpdatedDateTime = e.UpdateDateTime,
                        Name = e.Name,
                        ShortBio = e.ShortBio,
                        AvatarImage = e.AvatarImage
                    }).OrderByDescending(e => e.CreatedDateTime).Take(10).ToList();
        }

        // OK
        public UserModel GetUser(int userId)
        {
            try
            {
                return uow.UserRepository
                          .SearchFor(e => e.Id == userId)
                          .Select(e => new UserModel()
                          {
                              Id = e.Id,
                              Username = e.Username,
                              IsVerified = e.IsVerified,
                              VerificationToken = e.VerificationToken,
                              CreatedDateTime = e.CreatedDateTime,
                              UpdatedDateTime = e.UpdateDateTime,
                              Name = e.Name,
                              ShortBio = e.ShortBio,
                              AvatarImage = e.AvatarImage
                          }).SingleOrDefault();
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException("User lookup failed", ex);
            }
        }

        // OK
        public UserModel GetUserByUsername(string userName)
        {
            try
            {
                return uow.UserRepository
                          .SearchFor(e => e.Username == userName)
                          .Select(e => new UserModel()
                            {
                                Id = e.Id,
                                Username = e.Username,
                                IsVerified = e.IsVerified,
                                VerificationToken = e.VerificationToken,
                                CreatedDateTime = e.CreatedDateTime,
                                UpdatedDateTime = e.UpdateDateTime,
                                Name = e.Name,
                                ShortBio = e.ShortBio,
                                AvatarImage = e.AvatarImage,
                                IsAdmin = e.IsAdmin
                            }).SingleOrDefault();
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException("User lookup failed", ex);
            }
        }

        // OK
        public void CreateUser(UserModel source)
        {
            try
            {
                var _user = new User()
                {
                    Username = source.Username,
                    IsVerified = true,
                    VerificationToken = "",
                    //VerificationToken = this.generateVerificationToken(),
                    CreatedDateTime = DateTime.Now,
                    UpdateDateTime = DateTime.Now,
                    Name = source.Name,
                    //Name = "Anonymous User",
                    ShortBio = "",
                    AvatarImage = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSODALYDYo2dqN0DG_kPNi2X7EAy1K8SpRRZQWkNv9alC62IHggOw"
                };

                uow.UserRepository.Insert(_user, true);
            }
            catch (Exception)
            {
                throw;
            }
        }

        // OK
        public bool UpdateUser(UserModel source, ClaimsIdentity identity)
        {
            try
            {
                User _user;
                    
                try
                {
                    _user = uow.UserRepository
                               .SearchFor(e => e.Username == identity.Name)
                               .SingleOrDefault();
                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidOperationException("User lookup for requestor Id failed", ex);
                }

                if (_user == null) return false; 
                
                //update user from UserModel
                _user.UpdateDateTime = DateTime.Now;
                _user.Name = source.Name;
                _user.ShortBio = source.ShortBio;
                _user.AvatarImage = source.AvatarImage;

                uow.UserRepository.Update(_user, true);
                
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public IList<UserModel> GetByName(string searchTerm)
        {
            return uow.UserRepository
                      .SearchFor(e => e.Name.Contains(searchTerm))
                      .Select(e => new UserModel()
                      {
                          Id = e.Id,
                          Username = e.Username,
                          IsVerified = e.IsVerified,
                          CreatedDateTime = e.CreatedDateTime,
                          UpdatedDateTime = e.UpdateDateTime,
                          Name = e.Name,
                          ShortBio = e.ShortBio,
                          AvatarImage = e.AvatarImage,
                          IsAdmin = e.IsAdmin

                      }).OrderByDescending(e => e.CreatedDateTime).ToList();

        }

        // OK
        public StatusCodes DeactivateUserAccount(ClaimsIdentity identity, int userId)
        {
            try
            {
                var _user = uow.UserRepository.FindById(userId);

                //user not found
                if (_user == null)
                {
                    return StatusCodes.NOT_FOUND;
                }

                else
                {
                    // user found. is the user authorized? check this here
                    long requestorUserId;

                    try
                    {
                        requestorUserId = uow.UserRepository
                                             .SearchFor(e => e.Username == identity.Name)
                                             .Select(e => e.Id)
                                             .SingleOrDefault();
                    }
                    catch (InvalidOperationException ex)
                    {
                        throw new InvalidOperationException("User lookup for requestor Id failed", ex);
                    }

                    //var userIdClaim = identity.Claims
                    //.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);

                    if (userId != requestorUserId)
                    {
                        return StatusCodes.NOT_AUTHORIZED;
                    }

                    uow.UserRepository.Delete(_user);
                }

                return StatusCodes.OK;
            }
            catch (Exception)
            {
                throw;
            }
        }

        // OK
        public IList<ProjectModel> GetUserCreatedProjects(int userId)
        {
            var publicurl = UtilMethods.GetHostUrl();

            var _user = uow.UserRepository.FindById(userId);
            
            //user not found
            if (_user == null)
            {
                return new List<ProjectModel>() { };
            }

            else { 
                return uow.ProjectRepository
                          //.SearchFor(e => (e.UserId == userId && e.Status != "CRE" && e.Status != "FAI"))
                          .SearchFor(e => e.UserId == userId)
                          .Select(e => new ProjectModel()
                            {
                                Id = e.Id,
                                OwnerId = e.UserId,
                                OwnerName = e.User.Name,
                                ProjectCategoryId = e.ProjectCategoryId,
                                ProjectCategoryDesc = e.ProjectCategory.Name,
                                Title = e.Title,
                                Description = e.Description,
                                CreatedDate = e.CreatedDate,
                                UpdatedDate = e.UpdateDate,
                                FundingEndDate = e.FundingEndDate,
                                FundingGoal = e.FundingGoal,
                                Status = e.Status,
                                AttachmentSetId = e.AttachmentSetId,
                                MainPhoto = e
                                            .AttachmentSet
                                            .Attachments
                                            .Where(f => f.FilePath != null)
                                            .OrderBy(o => o.OrderNo).Select(g => g.FilePath)
                                            .FirstOrDefault()?.Replace("D:\\home\\site\\wwwroot\\", publicurl)
                          }).OrderByDescending(e => e.UpdatedDate).ToList();
            }
        }

        // OK
        public IList<ProjectModel> GetCurrentLoggedInUserCreatedProjects(ClaimsIdentity identity)
        {
            var publicurl = UtilMethods.GetHostUrl();

            long currentUserId;
            
            try
            {
                currentUserId = uow.UserRepository
                                 .SearchFor(e => e.Username == identity.Name)
                                 .Select(e => e.Id)
                                 .SingleOrDefault();
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException("User lookup for current logged in User Id failed", ex);
            }
            
            return uow.ProjectRepository
                    .SearchFor(e => e.UserId == currentUserId)
                    .Select(e => new ProjectModel()
                    {
                        Id = e.Id,
                        OwnerId = e.UserId,
                        OwnerName = e.User.Name,
                        ProjectCategoryId = e.ProjectCategoryId,
                        ProjectCategoryDesc = e.ProjectCategory.Name,
                        Title = e.Title,
                        Description = e.Description,
                        CreatedDate = e.CreatedDate,
                        UpdatedDate = e.UpdateDate,
                        FundingEndDate = e.FundingEndDate,
                        FundingGoal = e.FundingGoal,
                        AttachmentSetId = e.AttachmentSetId,
                        Status = e.Status,
                        MainPhoto = e
                                    .AttachmentSet
                                    .Attachments
                                    .Where(f => f.FilePath != null)
                                    .OrderBy(o => o.OrderNo).Select(g => g.FilePath)
                                    .FirstOrDefault()?.Replace("D:\\home\\site\\wwwroot\\", publicurl)
                    }).OrderByDescending(e => e.UpdatedDate).ToList();

        }

        // OK
        public IList<ProjectModel> GetUserFundedProjects(int userId)
        {
            var publicurl = UtilMethods.GetHostUrl();

            var _user = uow.UserRepository.FindById(userId);

            //user not found
            if (_user == null)
            {
                return new List<ProjectModel>() { };
            }

            else
            {
                //get user funded projects that have status = "COM"
                using (var ctx = new VivaWalletEntities())
                {

                    //first return rows as IEnumerable - Reason? A user may have backed this project
                    //that completed multiple times 
                    //as a result we may end have many same rows
                    //create a function distinctBy to remove same entries from the IEnumerable

                    IOrderedQueryable<ProjectModel> userFundingsOrderedQueryable = ctx.UserFundings
                        .Join(ctx.FundingPackages, uf => uf.FundingPackageId, fp => fp.Id, (uf, fp) => new { uf, fp })
                        .Join(ctx.Projects, uffp => uffp.fp.ProjectId, pr => pr.Id, (uffp, pr) => new { uffp.fp, uffp.uf, pr })
                        .Where(uffppr => (uffppr.uf.UserId == userId))
                        .Select(uffppr => new ProjectModel()
                        {
                            Id = uffppr.pr.Id,
                            OwnerId = uffppr.pr.UserId,
                            OwnerName = uffppr.pr.User.Name,
                            ProjectCategoryId = uffppr.pr.ProjectCategoryId,
                            ProjectCategoryDesc = uffppr.pr.ProjectCategory.Name,
                            Title = uffppr.pr.Title,
                            Description = uffppr.pr.Description,
                            CreatedDate = uffppr.pr.CreatedDate,
                            UpdatedDate = uffppr.pr.UpdateDate,
                            FundingEndDate = uffppr.pr.FundingEndDate,
                            FundingGoal = uffppr.pr.FundingGoal,
                            Status = uffppr.pr.Status,
                            AttachmentSetId = uffppr.pr.AttachmentSetId,
                            MainPhoto = uffppr.pr
                                                .AttachmentSet
                                                .Attachments
                                                .Where(f => f.FilePath != null)
                                                .OrderBy(o => o.OrderNo).Select(g => g.FilePath)
                                                .FirstOrDefault()
                        }).OrderByDescending(e => e.UpdatedDate);

                    IEnumerable<ProjectModel> userFundings = userFundingsOrderedQueryable.AsEnumerable();

                    foreach (var uf in userFundings)
                    {
                        if(uf.MainPhoto != null)
                        {
                            uf.MainPhoto = uf.MainPhoto.Replace("D:\\home\\site\\wwwroot\\", publicurl);
                        }
                    }

                    //return the filtered set of rows as a IList for the view to render
                    return UserRepository.DistinctBy(userFundings, p => p.Id).ToList();
                }
            }
        }

        public IList<ProjectUpdateModel> GetCurrentUserFundedProjectsLatestUpdates(ClaimsIdentity identity)
        {
            long currentUserId;

            try
            {
                currentUserId = uow.UserRepository
                                 .SearchFor(e => e.Username == identity.Name)
                                 .Select(e => e.Id)
                                 .SingleOrDefault();
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException("User lookup for current logged in User Id failed in GetUserFundedProjectsCompletedNotifications", ex);
            }
            
            //get user funded projects updates
            using (var ctx = new VivaWalletEntities())
            {

                //first return rows as IEnumerable - Reason? A user may have backed this project
                //that completed multiple times 
                //as a result we may end have many same rows
                //create a function distinctBy to remove same entries from the IEnumerable

                IOrderedQueryable<ProjectUpdateModel> userFundingsOrderedQueryable = ctx.UserFundings
                    .Join(ctx.FundingPackages, uf => uf.FundingPackageId, fp => fp.Id, (uf, fp) => new { uf, fp })
                    .Join(ctx.Projects, uffp => uffp.fp.ProjectId, pr => pr.Id, (uffp, pr) => new { uffp.fp, uffp.uf, pr })
                    .Join(ctx.ProjectUpdates, uffppr => uffppr.pr.Id, pu => pu.ProjectId, (uffppr, pu) => new { uffppr.pr, uffppr.fp, uffppr.uf, pu })
                    .Where(uffpprpu => (uffpprpu.uf.UserId == currentUserId) && (uffpprpu.pr.Id == uffpprpu.pu.ProjectId))
                    .Select(uffpprpu => new ProjectUpdateModel()
                    {
                        Id = uffpprpu.pr.Id,
                        ProjectId = uffpprpu.pr.Id,
                        AttachmentSetId = uffpprpu.pr.AttachmentSetId,
                        Title = uffpprpu.pu.Title,
                        Description = uffpprpu.pu.Description,
                        WhenDateTime = uffpprpu.pu.WhenDateTime
                    }).OrderByDescending(e => e.WhenDateTime);

                IEnumerable<ProjectUpdateModel> userFundings = userFundingsOrderedQueryable.AsEnumerable();

                //return the filtered set of rows as a IList for the view to render
                return UserRepository.DistinctBy(userFundings, p => p.Id).ToList();
            }
            
        }
        
        public IList<ProjectModel> GetUserFundedCompletedProjects(ClaimsIdentity identity, bool showAll)
        {

            var publicurl = UtilMethods.GetHostUrl();

            long currentUserId;

            try
            {
                currentUserId = uow.UserRepository
                                 .SearchFor(e => e.Username == identity.Name)
                                 .Select(e => e.Id)
                                 .SingleOrDefault();
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException("User lookup for current logged in User Id failed in GetUserFundedProjectsCompletedNotifications", ex);
            }
            
            //get user funded projects that have status = "COM"
            using (var ctx = new VivaWalletEntities())
            {

                //first return rows as IEnumerable - Reason? A user may have backed this project
                //that completed multiple times 
                //as a result we may end have many same rows
                //create a function distinctBy to remove same entries from the IEnumerable

                IOrderedQueryable<ProjectModel> userFundingsOrderedQueryable = ctx.UserFundings
                    .Join(ctx.FundingPackages, uf => uf.FundingPackageId, fp => fp.Id, (uf, fp) => new { uf, fp })
                    .Join(ctx.Projects, uffp => uffp.fp.ProjectId, pr => pr.Id, (uffp, pr) => new { uffp.fp, uffp.uf, pr })
                    .Where(uffppr => (uffppr.uf.UserId == currentUserId && uffppr.pr.Status == "COM"))
                    .Select(uffppr => new ProjectModel()
                    {
                        Id = uffppr.pr.Id,
                        OwnerId = uffppr.pr.UserId,
                        OwnerName = uffppr.pr.User.Name,
                        ProjectCategoryId = uffppr.pr.ProjectCategoryId,
                        ProjectCategoryDesc = uffppr.pr.ProjectCategory.Name,
                        Title = uffppr.pr.Title,
                        Description = uffppr.pr.Description,
                        CreatedDate = uffppr.pr.CreatedDate,
                        UpdatedDate = uffppr.pr.UpdateDate,
                        FundingEndDate = uffppr.pr.FundingEndDate,
                        FundingGoal = uffppr.pr.FundingGoal,
                        Status = uffppr.pr.Status,
                        AttachmentSetId = uffppr.pr.AttachmentSetId,
                        MainPhoto = uffppr.pr
                                            .AttachmentSet
                                            .Attachments
                                            .Where(f => f.FilePath != null)
                                            .OrderBy(o => o.OrderNo).Select(g => g.FilePath)
                                            .FirstOrDefault()
                    }).OrderByDescending(e => e.UpdatedDate);

                IEnumerable<ProjectModel> userFundings;

                //if showAll true show all completed projects
                if (showAll)
                {
                    userFundings = userFundingsOrderedQueryable.AsEnumerable();
                }

                //else show top most recent completed
                else
                {
                    userFundings = userFundingsOrderedQueryable.Take(5).AsEnumerable();
                }

                foreach (var uf in userFundings)
                {
                    if (uf.MainPhoto != null)
                    {
                        uf.MainPhoto = uf.MainPhoto.Replace("D:\\home\\site\\wwwroot\\", publicurl);
                    }
                }

                //return the filtered set of rows as a IList for the view to render
                return UserRepository.DistinctBy(userFundings, p => p.Id).ToList();
            }

        }
        
        public static IEnumerable<TSource> 
            DistinctBy<TSource, TKey>
            (IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        // OK
        public AdminPanelViewModel GetAdminPanelInfo(ClaimsIdentity identity)
        {
            try
            {
                long requestorUserId;

                try
                {
                    requestorUserId = uow.UserRepository
                                            .SearchFor(e => e.Username == identity.Name)
                                            .Select(e => e.Id)
                                            .SingleOrDefault();
                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidOperationException("User lookup for requestor Id failed", ex);
                }

                var _adminUser = uow.UserRepository.FindById(requestorUserId);

                //user not found
                if (_adminUser == null)
                {
                    return null;
                }

                //user not admin
                if (_adminUser.IsAdmin == false)
                {
                    return null;
                }

                //COLLECT ADMIN PANEL DATA
                AdminPanelViewModel adminPanelView = new AdminPanelViewModel();
                adminPanelView.NoOfTotalUsers = this.GetNoOfTotalUsersRegistered();
                adminPanelView.NoOfTotalProjectUpdates = this.GetNoOfTotalProjectUpdates();
                adminPanelView.NoOfProjectsPerCategory = this.GetNoOfProjectsPerCategory();
                adminPanelView.NoOfProjectsPerStatus = this.GetNoOfProjectsPerStatus();
                adminPanelView.GlobalProjectStats = this.GetGlobalProjectStats();

                return adminPanelView;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private int GetNoOfTotalUsersRegistered()
        {
            return uow.UserRepository.All().Count();
        }

        private int GetNoOfTotalProjectUpdates()
        {
            return uow.ProjectUpdateRepository.All().Count();
        }

        private IList<ProjectCountByCategoryModel> GetNoOfProjectsPerCategory()
        {
            using (var ctx = new VivaWalletEntities())
            {
                return ctx.Database.SqlQuery<ProjectCountByCategoryModel>(
                        @" 
                            SELECT 
	                            pc.Name CategoryName,
	                            COUNT(pc.Id) NoOfProjects
                            FROM 
	                            Projects pr
                            LEFT JOIN
	                            ProjectCategories pc
                            ON 
	                            pr.ProjectCategoryId = pc.Id
                            GROUP BY 
	                            pc.Name
                        "
                    ).ToList<ProjectCountByCategoryModel>();
            }
        }

        private IList<ProjectCountByStatusModel> GetNoOfProjectsPerStatus()
        {
            using (var ctx = new VivaWalletEntities())
            {
                return ctx.Database.SqlQuery<ProjectCountByStatusModel>(
                        @" 
                            SELECT 
	                            pr.Status ProjectStatus,
                                COUNT(pr.Id) NoOfProjects
                            FROM 
	                            Projects pr
                            GROUP BY
	                            pr.Status
                        "
                    ).ToList<ProjectCountByStatusModel>();
            }
        }

        private GlobalProjectStatsModel GetGlobalProjectStats()
        {
            using (var ctx = new VivaWalletEntities())
            {
                return ctx.Database.SqlQuery<GlobalProjectStatsModel>(
                        @" 
                            SELECT 
	                            SUM(ps.BackersNo) NoOfTotalBackings,
	                            SUM(ps.MoneyPledged) NoOfTotalMoneyPledged,
	                            SUM(ps.SharesNo) NoOfTotalExternalShares,
	                            SUM(ps.CommentsNo) NoOfTotalProjectComments
                            FROM 
	                            ProjectStats ps
                        "
                    ).FirstOrDefault();
            }
        }

        public void Dispose()
        {
            uow.Dispose();
        }

        public enum StatusCodes
        {
            NOT_FOUND = 0,
            NOT_AUTHORIZED = 1,
            OK = 2
        };
    }
}
