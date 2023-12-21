using IPDImexWebsite.Controllers.CookiesAPI;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.AspNetCore.Http.Features;

namespace IPDImexWebsiteUnitTests.Controllers
{
    [TestFixture]
    internal class CookiesControllerTests
    {
        private Mock<ILogger<CookiesController>> _logger;
        private Mock<IHttpContextAccessor> _accesor;
        private CookiesController _controller;  

        //
        private Mock<HttpContext> _context;
        private Mock<IFeatureCollection> _features;
        private Mock<ITrackingConsentFeature> _consentFeature;

        [SetUp]
        public void SetUp()
        {
            _context = new Mock<HttpContext>();
            _features = new Mock<IFeatureCollection>();
            _consentFeature = new Mock<ITrackingConsentFeature>();
            _context.SetupGet(x => x.Features).Returns(_features.Object);


            _accesor = new Mock<IHttpContextAccessor>();
            _accesor.SetupGet(x => x.HttpContext).Returns(_context.Object); 
            _logger = new Mock<ILogger<CookiesController>>();
            _controller = new CookiesController(_logger.Object, _accesor.Object);
        }

        [Test]
        public void Consent_ModelStateIsNotValid_ReturnBadRequest()
        {
            //arrange
            _controller.ModelState.AddModelError("error", "custom error");

            //act
            var result = _controller.Consent(default) as BadRequestObjectResult;

            //arrange
            Assert.That(result?.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            _accesor.Verify(x => x.HttpContext, Times.Never());
        }

        [Test]
        public void Consent_ConsentIsGranted_ReturnOk()
        {
            //arrange
            _features.Setup(x => x.Get<ITrackingConsentFeature>()).Returns(_consentFeature.Object);

            //act
            var result = _controller.Consent(true) as OkObjectResult;

            //arrange
            _features.Verify(x => x.Get<It.IsAnyType>(), Times.Once());
            Assert.That(result?.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            _accesor.Verify(x => x.HttpContext, Times.Once());
            _consentFeature.Verify(x => x.GrantConsent(),Times.Once());
            _consentFeature.Verify(x => x.WithdrawConsent(), Times.Never());
        }
        [Test]
        public void Consent_ConsentIsNotGranted_ReturnOk()
        {
            //arrange
            _features.Setup(x => x.Get<ITrackingConsentFeature>()).Returns(_consentFeature.Object);

            //act
            var result = _controller.Consent(false) as OkObjectResult;

            //arrange
            _features.Verify(x => x.Get<It.IsAnyType>(), Times.Once());
            Assert.That(result?.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            _accesor.Verify(x => x.HttpContext, Times.Once());
            _consentFeature.Verify(x => x.GrantConsent(), Times.Never());
            _consentFeature.Verify(x => x.WithdrawConsent(), Times.Once());
        }
    }
}
