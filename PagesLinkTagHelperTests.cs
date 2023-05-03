using IPDImexWebsite.Infrastructure;
using IPDImexWebsite.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPDImexWebsiteUnitTests
{
    [TestFixture]
    internal class PagesLinkTagHelperTests
    {
        [Test]
        public void Process_GenerateLinks_ReturnThreePagesInsideHtml()
        {
            //arrange
            //mock the IUrlHelper first because IUrlHelperFactory needs to return an IUrlHelper object
            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.SetupSequence(x => x.Action(It.IsAny<UrlActionContext>())).Returns("/Test/Page1")
                                                                                              .Returns("/Test/Page2")
                                                                                              .Returns("/Test/Page3");

            var mockIUrlHelperFactory = new Mock<IUrlHelperFactory>();
            mockIUrlHelperFactory.Setup(x => x.GetUrlHelper(It.IsAny<ActionContext>())).Returns(mockUrlHelper.Object);

            //there is no View context so mock it
            var viewContext = new Mock<ViewContext>();
            var pagination = new Pagination
            {
                ItemsPerPage = 9,
                CurrentPage = 2,
                TotalItems = 27
            };

            PagesLinkTagHelper pageHelper = new PagesLinkTagHelper(mockIUrlHelperFactory.Object)
            {
                PageAction = "ActionTest",
                ViewContext = viewContext.Object,
                PaginationModel = pagination,
                CurrentLinkStyle ="currentStyle",
                ButtonStyle = "buttonStyle",
                NextButtonServiceClass = "class",
                PreviousButtonServiceClass = "class",
                LinkStyle = "linkStyle",

            };

            var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "");

            var content = new Mock<TagHelperContent>();
            var output = new TagHelperOutput("div", new TagHelperAttributeList(), (c, e) => Task.FromResult(content.Object));

            //act
            pageHelper.Process(context, output);

            //assert
            Assert.That(output.Content.GetContent(), Does.Contain("Test/Page1"));
            Assert.That(output.Content.GetContent(), Does.Contain("Test/Page2"));
            Assert.That(output.Content.GetContent(), Does.Contain("Test/Page3"));
            Assert.That(output.Content.GetContent(), Does.Contain("</a>"));
            Assert.That(output.Content.GetContent(), Does.Contain("buttonStyle"));
            Assert.That(output.Content.GetContent(), Does.Contain("linkStyle"));
            Assert.That(output.Content.GetContent(), Does.Contain("class"));
        }
    }
}
