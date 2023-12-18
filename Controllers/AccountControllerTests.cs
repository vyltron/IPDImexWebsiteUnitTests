using IPDImexWebsite.Controllers;
using IPDImexWebsite.CustomServices;
using IPDImexWebsite.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace IPDImexWebsiteUnitTests.Controllers
{
    [TestFixture]
    internal class AccountControllerTests
    {
        private Mock<IHttpContextAccessor> _accesor;
        private Mock<UserManager<IdentityUser>> _userManager;
        private Mock<SignInManager<IdentityUser>> _signInManager;
        private Mock<ILogger<AccountController>> _logger;
        private Mock<IEmailService> _emailService;
        private AccountController _accountController;
        [SetUp]
        public void SetUp()
        {
            _accesor = new Mock<IHttpContextAccessor>();
            _userManager = new Mock<UserManager<IdentityUser>>(new Mock<IUserStore<IdentityUser>>().Object, null, null, null, null, null, null, null, null);
            _signInManager = new Mock<SignInManager<IdentityUser>>
                (_userManager.Object, _accesor.Object, new Mock<IUserClaimsPrincipalFactory<IdentityUser>>().Object, null, null, null, null);
            _logger = new Mock<ILogger<AccountController>>();
            _emailService = new Mock<IEmailService>();

            _accountController = new AccountController(_userManager.Object, _signInManager.Object, _logger.Object,
                                                       _emailService.Object,_accesor.Object);
        }

        #region CreateUser
        [Test]
        public void CreateUser_UserIsNotAUthorizedForThis_RedirectToAdminInfoPage()
        {
            //arrange
            var httpContext = new Mock<HttpContext>();
            var claim = new Mock<ClaimsPrincipal>();
            httpContext.SetupGet(x => x.User).Returns(claim.Object);
            claim.Setup(x => x.IsInRole(It.IsAny<string>())).Returns(false);
            _accesor.SetupGet(x => x.HttpContext).Returns(httpContext.Object);

            //act
            var result = _accountController.CreateUser() as RedirectToPageResult;

            //arrange
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
        }

        [Test]  
        public async Task CreateUserPost_UserIsNotAuthorized_RedirectToAdminInfo()
        {
            //arrange
            var httpContext = new Mock<HttpContext>();
            var claim = new Mock<ClaimsPrincipal>();
            httpContext.SetupGet(x => x.User).Returns(claim.Object);
            claim.Setup(x => x.IsInRole(It.IsAny<string>())).Returns(false);
            _accesor.SetupGet(x => x.HttpContext).Returns(httpContext.Object);

            //act
            var result = await _accountController.CreateUserPost(new CreateUserModel()) as RedirectToPageResult;

            //arrange
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task CreateUserPost_ModelIsNotValid_RedirectToAdminInfo()
        {
            //arrange
            var httpContext = new Mock<HttpContext>();
            var claim = new Mock<ClaimsPrincipal>();
            httpContext.SetupGet(x => x.User).Returns(claim.Object);
            claim.Setup(x => x.IsInRole(It.IsAny<string>())).Returns(true);
            _accesor.SetupGet(x => x.HttpContext).Returns(httpContext.Object);
          
            _accountController.ModelState.AddModelError("error", "customError");

            //act
            var result = await _accountController.CreateUserPost(new CreateUserModel()) as ViewResult;

            //arrange
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("CreateUser"));
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task CreateUserPost_USerNameAlreadyExist_ReturnView()
        {
            //arrange
            _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(new IdentityUser());

            var httpContext = new Mock<HttpContext>();
            var claim = new Mock<ClaimsPrincipal>();
            httpContext.SetupGet(x => x.User).Returns(claim.Object);
            claim.Setup(x => x.IsInRole(It.IsAny<string>())).Returns(true);
            _accesor.SetupGet(x => x.HttpContext).Returns(httpContext.Object);

            //act
            var result = await _accountController.CreateUserPost(new CreateUserModel()) as ViewResult;

            //arrange
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("CreateUser"));
            Assert.That(result.ViewData.ModelState.ContainsKey("userNameExist"), Is.True);
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task CreateUserPost_EmailAlreadyExist_ReturnView()
        {
            //arrange
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(new IdentityUser());

            var httpContext = new Mock<HttpContext>();
            var claim = new Mock<ClaimsPrincipal>();
            httpContext.SetupGet(x => x.User).Returns(claim.Object);
            claim.Setup(x => x.IsInRole(It.IsAny<string>())).Returns(true);
            _accesor.SetupGet(x => x.HttpContext).Returns(httpContext.Object);

            //act
            var result = await _accountController.CreateUserPost(new CreateUserModel()) as ViewResult;

            //arrange
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("CreateUser"));
            Assert.That(result.ViewData.ModelState.ContainsKey("emailExist"), Is.True);
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.CreateAsync(It.IsAny<IdentityUser>()), Times.Never());
        }

        [Test]
        [TestCase("test")]
        [TestCase("Testpas")]
        [TestCase("TestP@")]
        [TestCase("TestP2")]
        public async Task CreateUserPost_PasswordIsNotValid_ReturnView(string password)
        {
            //arrange
            var httpContext = new Mock<HttpContext>();
            var claim = new Mock<ClaimsPrincipal>();
            httpContext.SetupGet(x => x.User).Returns(claim.Object);
            claim.Setup(x => x.IsInRole(It.IsAny<string>())).Returns(true);
            _accesor.SetupGet(x => x.HttpContext).Returns(httpContext.Object);

            var model = new CreateUserModel
            {
                UserName = "Test",
                Email = "Test@",
                Password = password,
                ConfirmPassword = "Test"
            };

            //act
            var result = await _accountController.CreateUserPost(model) as ViewResult;

            //arrange
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("CreateUser"));
            Assert.That(result.ViewData.ModelState.ContainsKey("weakPassword"), Is.True);
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.CreateAsync(It.IsAny<IdentityUser>()), Times.Never());
        }

        [Test]
        public async Task CreateUserPost_PasswordsDontMatch_ReturnView()
        {
            //arrange
            var httpContext = new Mock<HttpContext>();
            var claim = new Mock<ClaimsPrincipal>();
            httpContext.SetupGet(x => x.User).Returns(claim.Object);
            claim.Setup(x => x.IsInRole(It.IsAny<string>())).Returns(true);
            _accesor.SetupGet(x => x.HttpContext).Returns(httpContext.Object);

            var model = new CreateUserModel
            {
                UserName = "Test",
                Email = "Test@",
                Password = "Test@P24",
                ConfirmPassword = "Test@P23"
            };

            //act
            var result = await _accountController.CreateUserPost(model) as ViewResult;

            //arrange
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("CreateUser"));
            Assert.That(result.ViewData.ModelState.ContainsKey("passwordsDoNotMatch"), Is.True);
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.CreateAsync(It.IsAny<IdentityUser>()), Times.Never());
        }

        [Test]
        public async Task CreateUserPost_NewUserAddedAndSendingEmailThrowsExceptionsButStillWorks_ReturnView()
        {
            //arrange
            var httpContext = new Mock<HttpContext>();
            var claim = new Mock<ClaimsPrincipal>();
            httpContext.SetupGet(x => x.User).Returns(claim.Object);
            claim.Setup(x => x.IsInRole(It.IsAny<string>())).Returns(true);
            _accesor.SetupGet(x => x.HttpContext).Returns(httpContext.Object);

            _emailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception("error"));

            var model = new CreateUserModel
            {
                UserName = "Test",
                Email = "Test@",
                Password = "Test@P24",
                ConfirmPassword = "Test@P24"
            };

            //act
            var result = await _accountController.CreateUserPost(model) as ViewResult;

            //arrange
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("CreateUser"));
            Assert.That(result.ViewData["UserAdded"], Is.EqualTo("Utilizatorul a fost adaugat cu succes!"));
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()), Times.Once());
        }
        #endregion
        #region Reset Password
        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("Test")]
        public async Task ForgotPassword_IsNullOrEmpty_ReturnView(string email)
        {
            //act
            var result = await _accountController.ForgotPassword(email) as ViewResult;

            //Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("EmailCheck"));
            Assert.That(result.ViewData.ModelState.ContainsKey("emptyEmail"), Is.True);
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public async Task ForgotPassword_UserIsNotFoundSendAFrendlyMessage_ReturnView()
        {
            //arrange
            IdentityUser user = default!;
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

            //act
            var result = await _accountController.ForgotPassword("email@test") as RedirectToPageResult;

            //Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.PageName, Is.EqualTo("/AdminInfo"));
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<IdentityUser>()), Times.Never());
        }

        [Test]
        public async Task ForgotPassword_UserIsFoundGeneratePasswordResetLink_ReturnAdminInfo()
        {
            //arrange
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(new IdentityUser());
            var mockUrlHelper = new Mock<IUrlHelper>();
            _accountController.Url = mockUrlHelper.Object;
            mockUrlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("/test/link/test");
           
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(x => x.Scheme).Returns("http");

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(x => x.Request).Returns(mockRequest.Object);
            _accesor.SetupGet(x => x.HttpContext).Returns(httpContext.Object);

            //act
            var result = await _accountController.ForgotPassword("email@test") as RedirectToPageResult;

            //Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.PageName, Is.EqualTo("/AdminInfo"));
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<IdentityUser>()), Times.Once());
        }

        [Test]
        [TestCase(null, null)]
        [TestCase("","")]
        [TestCase("test",null)]
        [TestCase(null,"test")]
        public async Task ResetPassword_TokenOrEmailIsNullOrEmpty_ReturnRedirectToPage(string token, string email)
        {
            //act
            var result = await _accountController.ResetPassword(email, token) as RedirectToPageResult;

            //arrange
            Assert.That(result, Is.Not.Null);
            Assert.That(result.PageName, Is.EqualTo("/AdminInfo"));
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task ResetPassword_UserIsNotFound_ReturnAdminInfo()
        {
            //arrange
            IdentityUser user = default!;
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

            //act
            var result = await _accountController.ResetPassword("test", "test") as RedirectToPageResult;

            //arrange
            Assert.That(result, Is.Not.Null);
            Assert.That(result.PageName, Is.EqualTo("/AdminInfo"));
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.VerifyUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task ResetPassword_TokenIsExpired_ReturnAdminInfo()
        {
            //arrange
            IdentityUser user = new IdentityUser();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.VerifyUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            //act
            var result = await _accountController.ResetPassword("test", "test") as RedirectToPageResult;

            //arrange
            Assert.That(result, Is.Not.Null);
            Assert.That(result.PageName, Is.EqualTo("/AdminInfo"));
            StringAssert.Contains("expirat", result.RouteValues?["info"] as string);
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.VerifyUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.GenerateUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task ResetPassword_TokenIsvalid_ReturnResetPasswordView()
        {
            //arrange
            IdentityUser user = new IdentityUser();
            var tokenTest = "testToken";
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.VerifyUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _userManager.Setup(x => x.GenerateUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(tokenTest);
            //act
            var result = await _accountController.ResetPassword("test", "test") as ViewResult;
            var model = result?.ViewData.Model as ResetPasswordViewModel ?? new();

            //arrange
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("ResetPassword"));
            Assert.That(model.Email, Is.EqualTo("test"));
            Assert.That(model.Token, Is.EqualTo(tokenTest));
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.VerifyUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.GenerateUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [Test]
        public async Task ProceedToChangePassword_EmailIsNotFoundInDB_ReturnAdminInfoFriendlyMessage()
        {
            //arranage
            IdentityUser user = default!;
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

            //act
            var result = await _accountController.ProceedToChangePassword(new ResetPasswordViewModel()) as RedirectToPageResult;

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
            _userManager.Verify(x => x.VerifyUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task ProceedToChangePassword_TokenIsNotValid_ReturnAdminInfoFriendlyMessage()
        {
            //arranage
            IdentityUser user = new IdentityUser();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

            //act
            var result = await _accountController.ProceedToChangePassword(new ResetPasswordViewModel()) as RedirectToPageResult;

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
            StringAssert.Contains("Token-ul", result?.RouteValues?["info"] as string);
            _userManager.Verify(x => x.VerifyUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<IdentityUser>()), Times.Never());
        }
        [Test]
        public async Task ProceedToChangePassword_ModelisNotValid_ReturnView()
        {
            //arranage
            IdentityUser user = new IdentityUser();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.VerifyUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _accountController.ModelState.AddModelError("error", "custom error");

            //act
            var result = await _accountController.ProceedToChangePassword(new ResetPasswordViewModel
            {
                Email = "test@",
                Token = "test"
            }) as ViewResult;
            var model = result?.ViewData.Model as ResetPasswordViewModel ?? new();

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.ViewName, Is.EqualTo("ResetPassword"));
            Assert.That(model.Email, Is.EqualTo("test@"));
            Assert.That(model.Token , Is.EqualTo("test"));
            Assert.That(result.ViewData.ModelState.ContainsKey("error"));
            _userManager.Verify(x => x.VerifyUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<IdentityUser>()), Times.Never());
        }
        [Test]
        [TestCase("test")]
        [TestCase("Testpas")]
        [TestCase("TestP@")]
        [TestCase("TestP2")]
        public async Task ProceedToChangePassword_PasswordIsWeak_ReturnView(string password)
        {
            //arranage
            IdentityUser user = new IdentityUser();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.VerifyUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            //act
            var result = await _accountController.ProceedToChangePassword(new ResetPasswordViewModel
            {
                Email = "test@",
                Token = "test",
                Password = password
            }) as ViewResult;

            var model = result?.ViewData.Model as ResetPasswordViewModel ?? new();

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.ViewName, Is.EqualTo("ResetPassword"));
            Assert.That(model.Email, Is.EqualTo("test@"));
            Assert.That(model.Token, Is.EqualTo("test"));
            Assert.That(result.ViewData.ModelState.ContainsKey("weakPassword"), Is.True);
            _userManager.Verify(x => x.VerifyUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<IdentityUser>()), Times.Never());
        }
        [Test]
        public async Task ProceedToChangePassword_PasswordsDontMatch_ReturnView()
        {
            //arranage
            IdentityUser user = new IdentityUser();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.VerifyUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            //act
            var result = await _accountController.ProceedToChangePassword(new ResetPasswordViewModel
            {
                Email = "test@",
                Token = "test",
                Password = "Test@2",
                ConfirmPassword = "Test@3"
            }) as ViewResult;

            var model = result?.ViewData.Model as ResetPasswordViewModel ?? new();

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.ViewName, Is.EqualTo("ResetPassword"));
            Assert.That(model.Email, Is.EqualTo("test@"));
            Assert.That(model.Token, Is.EqualTo("test"));
            Assert.That(result.ViewData.ModelState.ContainsKey("passwordsDoNotMatch"), Is.True);
            _userManager.Verify(x => x.VerifyUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<IdentityUser>()), Times.Never());
        }
        [Test]
        public async Task ProceedToChangePassword_PasswordsMatched_ButResulstIsNotSucceded_ReturnAdminInfo()
        {
            //arranage
            IdentityUser user = new IdentityUser();
            var resetModel = new ResetPasswordViewModel
            {
                Email = "test@",
                Token = "test",
                Password = "Test@2",
                ConfirmPassword = "Test@2"
            };
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.VerifyUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _userManager.Setup(x => x.GeneratePasswordResetTokenAsync(It.IsAny<IdentityUser>())).ReturnsAsync("tokenTest");
            _userManager.Setup(x => x.ResetPasswordAsync(user, "tokenTest", resetModel.Password)).ReturnsAsync(new MockIdentityResult(false));
          
            //act
            var result = await _accountController.ProceedToChangePassword(resetModel) as RedirectToPageResult;
            

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
            StringAssert.Contains("aparut o eroare", result?.RouteValues?["info"] as string);
            _userManager.Verify(x => x.VerifyUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<IdentityUser>()), Times.Once());
            _userManager.Verify(x => x.ResetPasswordAsync(user, "tokenTest", resetModel.Password), Times.Once());
        }
        [Test]
        public async Task ProceedToChangePassword_PasswordsMatched_ResultSuccededPasswrodIschanged_ReturnAdminInfo()
        {
            //arranage
            IdentityUser user = new IdentityUser();
            var resetModel = new ResetPasswordViewModel
            {
                Email = "test@",
                Token = "test",
                Password = "Test@2",
                ConfirmPassword = "Test@2"
            };
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.VerifyUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _userManager.Setup(x => x.GeneratePasswordResetTokenAsync(It.IsAny<IdentityUser>())).ReturnsAsync("tokenTest");
            _userManager.Setup(x => x.ResetPasswordAsync(user, "tokenTest", resetModel.Password)).ReturnsAsync(new MockIdentityResult(true));

            //act
            var result = await _accountController.ProceedToChangePassword(resetModel) as RedirectToPageResult;


            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.PageName, Is.EqualTo("/AdminInfo"));
            StringAssert.Contains("schimbata cu succes", result?.RouteValues?["info"] as string);
            _userManager.Verify(x => x.VerifyUserTokenAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<IdentityUser>()), Times.Once());
            _userManager.Verify(x => x.ResetPasswordAsync(user, "tokenTest", resetModel.Password), Times.Once());
        }
        #endregion
        #region Login
        [Test]
        public void Login_UserIsAlreadyAuthenticated_redirectHimToControlPanel()
        {
            //arrange
            var mockIIdentity = new Mock<IIdentity>();
            mockIIdentity.SetupGet(x => x.IsAuthenticated).Returns(true);
            var claim = new Mock<ClaimsPrincipal>();
            claim.SetupGet(x => x.Identity).Returns(mockIIdentity.Object);
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(x => x.User).Returns(claim.Object);
            _accesor.Setup(x => x.HttpContext).Returns(httpContext.Object);
            //act
            var result = _accountController.Login(default!) as RedirectToActionResult;

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.ActionName, Is.EqualTo("ControlPanel"));
            Assert.That(result.ControllerName, Is.EqualTo("Administration"));
        }
        [Test]
        public void Login_ReturnLoginView()
        {
            //act
            var result = _accountController.Login("returnUrlTest") as ViewResult;
            var model = result?.ViewData?.Model as LoginViewModel ?? new();

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(model.ReturnUrl, Is.EqualTo("returnUrlTest"));
        }

        [Test]
        public async Task LoginUser_ModelIsNotValid_ReturnLoginView()
        {
            //arrange
            _accountController.ModelState.AddModelError("error", "customError");

            //act
            var result = await _accountController.LoginUser(new LoginViewModel()) as ViewResult;

            //assert
            Assert.That(result?.ViewName, Is.EqualTo("Login"));
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task LoginUser_UserIsNotFound_ReturnLoginView()
        {
            //arrange
            IdentityUser user = default!;
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);

            //act
            var result = await _accountController.LoginUser(new LoginViewModel()) as ViewResult;

            //assert
            Assert.That(result?.ViewName, Is.EqualTo("Login"));
            Assert.That(result?.ViewData.ModelState.ContainsKey("invalidCredentials"), Is.True);
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Once());
            _signInManager.Verify(x => x.SignOutAsync(), Times.Never());
        }
        [Test]  
        public async Task LoginUser_UserNameOrPasswordWrong_ReturnLoginView()
        {
            //arrange
            IdentityUser user = new IdentityUser();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
            var loginModel = new LoginViewModel
            {
                Password = "test",
                Name = "Test"
            };

            _signInManager.Setup(x => x.PasswordSignInAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), false, false)).ReturnsAsync(new MockSignInResult(false));

            //act
            var result = await _accountController.LoginUser(new LoginViewModel()) as ViewResult;

            //assert
            Assert.That(result?.ViewName, Is.EqualTo("Login"));
            Assert.That(result?.ViewData.ModelState.ContainsKey("invalidCredentials"), Is.True);
            _signInManager.Verify(x => x.SignOutAsync(), Times.Once());
            _signInManager.Verify(x => x.PasswordSignInAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), false, false), Times.Once());
        }
        [Test]
        public async Task LogOut_CanLogOut()
        {
           var result = await  _accountController.Logout() as RedirectToActionResult;

            Assert.That(result?.ActionName, Is.EqualTo("Login"));
        }
        #endregion
        #region Profile
        [Test]
        public async Task Profile_UserNotFound_ReturnToLogin()
        {
            //arrange
            IdentityUser user = default!;
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            //act
            var result = await _accountController.Profile() as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("Login"));
        }
        [Test]
        public async Task Profile_ReturnProfileView()
        {
            //arrange
            IdentityUser user = new IdentityUser
            {
                Email = "test@email",
                PhoneNumber = "phoneTest",
                UserName = "TestName"
            };
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            //act
            var result = (await _accountController.Profile() as ViewResult)?.ViewData.Model as ProfileEndpointViewModel ?? new();

            //assert
            Assert.That(result.Email, Is.EqualTo("test@email"));
            Assert.That(result.Phone, Is.EqualTo("phoneTest"));
            Assert.That(result.UserName, Is.EqualTo("TestName"));
        }
        [Test]
        public async Task ChangeUserPassword_UserIsNotFound_ReturnLoginView()
        {
            //arranage
            IdentityUser user = default!;
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            //act
            var result = await _accountController.ChangeUserPassword(new ProfileEndpointViewModel()) as RedirectToActionResult;

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.ActionName, Is.EqualTo("Login"));
            _signInManager.Verify(x => x.CheckPasswordSignInAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
        }
        [Test]
        public async Task ChangeUserPassword_ModelisNotValid_ReturnProfileView()
        {
            //arranage
            IdentityUser user = new IdentityUser
            {
                UserName = "test",
                Email = "test@",
                PhoneNumber = "testPhone"
            };
            
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _accountController.ModelState.AddModelError("error", "custom error");

            //act
            var result = await _accountController.ChangeUserPassword(new ProfileEndpointViewModel()) as ViewResult;
            var model = result?.ViewData.Model as ProfileEndpointViewModel ?? new();

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.ViewName, Is.EqualTo("Profile"));
            Assert.That(model.Email, Is.EqualTo("test@"));
            Assert.That(model.Phone, Is.EqualTo("testPhone"));
            Assert.That(model.UserName, Is.EqualTo("test"));
            Assert.That(result.ViewData.ModelState.ContainsKey("error"), Is.True);
            _signInManager.Verify(x => x.CheckPasswordSignInAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
        }
        [Test]
        [TestCase("test")]
        [TestCase("Testpas")]
        [TestCase("TestP@")]
        [TestCase("TestP2")]
        public async Task ChangeUserPassword_PasswordIsWeak_ReturnView(string password)
        {
            //arranage
            IdentityUser user = new IdentityUser
            {
                UserName = "test",
                Email = "test@",
                PhoneNumber = "testPhone"
            };
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            //act
            var result = await _accountController.ChangeUserPassword(new ProfileEndpointViewModel { NewPassword = password}) as ViewResult;
            var model = result?.ViewData.Model as ProfileEndpointViewModel ?? new();


            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.ViewName, Is.EqualTo("Profile"));
            Assert.That(model.Email, Is.EqualTo("test@"));
            Assert.That(model.Phone, Is.EqualTo("testPhone"));
            Assert.That(model.UserName, Is.EqualTo("test"));
            Assert.That(result.ViewData.ModelState.ContainsKey("weakPassword"), Is.True);
            _signInManager.Verify(x => x.CheckPasswordSignInAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
        }
        [Test]
        public async Task ChangeUserPassword_PasswordsDontMatch_ReturnView()
        {
            //arranage
            IdentityUser user = new IdentityUser
            {
                UserName = "test",
                Email = "test@",
                PhoneNumber = "testPhone"
            };
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            //act
            var result = await _accountController.ChangeUserPassword(new ProfileEndpointViewModel 
            { NewPassword = "Test@P2", RepeateNewPassword = "test" }) as ViewResult;
            var model = result?.ViewData.Model as ProfileEndpointViewModel ?? new();


            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.ViewName, Is.EqualTo("Profile"));
            Assert.That(model.Email, Is.EqualTo("test@"));
            Assert.That(model.Phone, Is.EqualTo("testPhone"));
            Assert.That(model.UserName, Is.EqualTo("test"));
            Assert.That(result.ViewData.ModelState.ContainsKey("passwordsDoNotMatch"), Is.True);
            _signInManager.Verify(x => x.CheckPasswordSignInAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
        }
        [Test]
        public async Task ChangeUserPassword_CurrentPasswordDontMatch_ReturnView()
        {
            //arranage
            IdentityUser user = new IdentityUser
            {
                UserName = "test",
                Email = "test@",
                PhoneNumber = "testPhone"
            };
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _signInManager.Setup(x => x.CheckPasswordSignInAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), false)).ReturnsAsync(new MockSignInResult (false));

            //act
            var result = await _accountController.ChangeUserPassword(new ProfileEndpointViewModel
            { NewPassword = "Test@P2", RepeateNewPassword = "Test@P2", OldPassword = "testOld"}) as ViewResult;
            var model = result?.ViewData.Model as ProfileEndpointViewModel ?? new();


            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.ViewName, Is.EqualTo("Profile"));
            Assert.That(model.Email, Is.EqualTo("test@"));
            Assert.That(model.Phone, Is.EqualTo("testPhone"));
            Assert.That(model.UserName, Is.EqualTo("test"));
            Assert.That(result.ViewData.ModelState.ContainsKey("oldPasswordWrong"), Is.True);
            _signInManager.Verify(x => x.CheckPasswordSignInAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once());
            _userManager.Verify(x => x.ChangePasswordAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task ChangeUserPassword_ChangePasswordDidnotSucceded_RedirectToAdminInfo()
        {
            //arranage
            IdentityUser user = new IdentityUser
            {
                UserName = "test",
                Email = "test@",
                PhoneNumber = "testPhone"
            };
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _signInManager.Setup(x => x.CheckPasswordSignInAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), false)).ReturnsAsync(new MockSignInResult(true));
            _userManager.Setup(x => x.ChangePasswordAsync(It.IsAny<IdentityUser>(),It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new MockIdentityResult(false));    

            //act
            var result = await _accountController.ChangeUserPassword(new ProfileEndpointViewModel
            { NewPassword = "Test@P2", RepeateNewPassword = "Test@P2", OldPassword = "testOld" }) as RedirectToPageResult;


            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.PageName, Is.EqualTo("/AdminInfo"));
            _signInManager.Verify(x => x.CheckPasswordSignInAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once());
            _userManager.Verify(x => x.ChangePasswordAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }
        [Test]
        public async Task ChangeUserPassword_Succeded_ReturnView()
        {
            //arranage
            IdentityUser user = new IdentityUser
            {
                UserName = "test",
                Email = "test@",
                PhoneNumber = "testPhone"
            };
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _signInManager.Setup(x => x.CheckPasswordSignInAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), false)).ReturnsAsync(new MockSignInResult(true));
            _userManager.Setup(x => x.ChangePasswordAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new MockIdentityResult(true));

            //act
            var result = await _accountController.ChangeUserPassword(new ProfileEndpointViewModel
            { NewPassword = "Test@P2", RepeateNewPassword = "Test@P2", OldPassword = "testOld" }) as ViewResult;
            var model = result?.ViewData.Model as ProfileEndpointViewModel ?? new();


            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.ViewName, Is.EqualTo("Profile"));
            Assert.That(model.Email, Is.EqualTo("test@"));
            Assert.That(model.Phone, Is.EqualTo("testPhone"));
            Assert.That(model.UserName, Is.EqualTo("test"));
            _signInManager.Verify(x => x.CheckPasswordSignInAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once());
            _userManager.Verify(x => x.ChangePasswordAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        #endregion
    }

    public class MockIdentityResult : IdentityResult
    {
        public MockIdentityResult(bool succeded) 
        {
            Succeeded = succeded;
        }
    }
    public class MockSignInResult : Microsoft.AspNetCore.Identity.SignInResult
    {
        public MockSignInResult(bool succeded)
        {
            Succeeded = succeded;
        }
    }
}
