using Castle.Core.Logging;
using IPDImexWebsite.Controllers;
using IPDImexWebsite.Models;
using IPDImexWebsite.Models.Repository;
using IPDImexWebsite.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;


namespace IPDImexWebsiteUnitTests.Controllers
{
    [TestFixture]
    internal class ProjectsAdministrationTests
    {
        private Mock<IRepositoryProject> _project;
        private Mock<ILogger<ProjectsAdministrationController>> _logger;
        private Mock<IHttpContextAccessor> _context;
        private ProjectsAdministrationController _controller;

        [SetUp]
        public void SetUp()
        {
            _project = new Mock<IRepositoryProject>();
            _logger = new Mock<ILogger<ProjectsAdministrationController>>();
            _context = new Mock<IHttpContextAccessor>();
            _controller = new ProjectsAdministrationController(_project.Object, _logger.Object, _context.Object);
        }


        public async IAsyncEnumerable<Project> MockGetAllProjectsWithoutPictures()
        {
            var projects = new List<Project>
            {
                    new Project
                    {
                        Id = 1,
                        Title = "Liceul Teoretic",
                        Description = "Cel mai best liceu"
                    },
                    new Project
                    {
                        Id = 2,
                        Title = "Liceul Luna",
                        Description = "Acesta este cel mai prost liceu"
                    },
                    new Project
                    {
                        Id = 3,
                        Title = "Gradinita din padure",
                        Description = "Gradinita cu gaini"
                    },
                    new Project
                    {
                        Id = 4,
                        Title = "Gradinita din oras",
                        Description = "Gradinita cu vaci"
                    }
            };

            foreach (var project in projects)
            {
                yield return project;
            }
            await Task.CompletedTask;
        }

        public List<Picture> GetMockPictures()
        {
            var pictures = new List<Picture>
            {
                new Picture
                {
                    Id = 1,
                    ImageName = "image1.jpeg",
                    ContentType = "image/jpeg",
                    Extension = "jpeg"
                },
                new Picture
                {
                    Id = 2,
                    ImageName = "image2.jpg",
                    ContentType = "image/jpeg",
                    Extension = "jpeg"
                },
                new Picture
                {
                    Id = 3,
                    ImageName = "image3.jpg",
                    ContentType = "image/jpeg",
                    Extension = "jpeg"
                },
                new Picture
                {
                    Id = 4,
                    ImageName = "image4.png",
                    ContentType = "image/png",
                    Extension = "png"
                }
            };
            return pictures;
        }


        public IFormFile CreateCustomFormFileBinaryDataContent(byte[] imageData, int length = 1 * 1024 * 1024, string fileName = "image1.jpeg", string contentType = "image/jpeg", string name = "file")
        {
            var stream = new MemoryStream(imageData);

            IFormFile formFile = new FormFile(stream, 0, length, name, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };

            return formFile;
        }


        #region region ProjectPanel, SearchProjects, DeleteProjects
        [Test]
        public async Task ProjectPanel_CanPaginate_ReturnSpecifiedPage()
        {
            //arrange 
            _project.Setup(x => x.GetProjectsWithPaginationInDescendingOrder(It.IsAny<int>(), It.IsAny<int>())).Returns(MockGetAllProjectsWithoutPictures());
            _project.Setup(x => x.GetProjectsCount()).Returns(async () => await Task.FromResult(4));

            //act
            var result = (await _controller.ProjectsPanel() as ViewResult)?.ViewData.Model as ProjectsAdministrationViewModel ?? new();
            var projects = result.Projects.ToList();
            //assert
            Assert.Multiple(() =>
            {
                Assert.That(projects, Has.Count.EqualTo(4));
                Assert.That(projects[0].Id, Is.EqualTo(1));
                Assert.That(projects[0].Description, Is.EqualTo("Cel mai best liceu"));
                Assert.That(result.TotalProjects, Is.EqualTo(4));
            });
        }
        [Test]
        public async Task ProjectPanel_PaginationHasTheRightProperties_PaginationObjectOK()
        {
            //arrange

            _project.Setup(x => x.GetProjectsWithPaginationInDescendingOrder(It.IsAny<int>(), It.IsAny<int>())).Returns(MockGetAllProjectsWithoutPictures());
            _project.Setup(x => x.GetProjectsCount()).Returns(async () => await Task.FromResult(4));

            //act
            var result = await _controller.ProjectsPanel(1) as ViewResult;
            var model = result?.ViewData?.Model as ProjectsAdministrationViewModel ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(model.Pagination.ItemsPerPage, Is.EqualTo(6));
                Assert.That(model.Pagination.CurrentPage, Is.EqualTo(1));
                Assert.That(model.Pagination.TotalItems, Is.EqualTo(4));
                Assert.That(model.Pagination.TotalPages, Is.EqualTo(1));
                Assert.That(result?.ViewData.ContainsKey("CurrentUrl"), Is.True);

            });
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        public async Task SearchProjects_SearchCruteriaIsNull_ReturnToActionProjectsPanel(string searchCriteria)
        {
            //act

            var result = await _controller.SearchProjects(searchCriteria) as RedirectToActionResult;

            //assert
            Assert.That(result!.ActionName, Is.EqualTo("ProjectsPanel"));
        }

        [Test]
        public async Task SearchProject_SearchCanPaginate_ReturnPanelView()
        {
            //arrange 
            _project.Setup(x => x.SearchProjectsAsync(It.IsAny<string>())).Returns(MockGetAllProjectsWithoutPictures());
            _project.Setup(x => x.GetProjectsCount()).Returns(async () => await Task.FromResult(4));

            //act
            var result = (await _controller.SearchProjects("test") as ViewResult)?.ViewData.Model as ProjectsAdministrationViewModel ?? new();
            var projects = result.Projects.ToList();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(projects, Has.Count.EqualTo(4));
                Assert.That(projects[0].Id, Is.EqualTo(4));
                Assert.That(projects[0].Description, Is.EqualTo("Gradinita cu vaci"));
                Assert.That(result.TotalProjects, Is.EqualTo(4));
            });
        }
        [Test]
        public async Task SearchProject_CanReturnCurrentUrlAndSearchCriteria_ReturnPanelVIew()
        {
            //arrange 
            _project.Setup(x => x.SearchProjectsAsync(It.IsAny<string>())).Returns(MockGetAllProjectsWithoutPictures());
            _project.Setup(x => x.GetProjectsCount()).Returns(async () => await Task.FromResult(4));

            //act
            var result = (await _controller.SearchProjects("test") as ViewResult)?.ViewData.Model as ProjectsAdministrationViewModel ?? new();
            var projects = result.Projects.ToList();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result.SearchCriteria, Is.EqualTo("test"));
                Assert.That(_controller.ViewBag.SearchCriteria, Is.EqualTo("test"));
                Assert.That(result.SearchCriteria, Is.EqualTo("test"));
                Assert.That(result.SearchRequest, Is.True);
            });
        }

        [Test]
        public async Task SearchProject_PaginationObjectIsSetUp_ReturnPanelVIew()
        {
            //arrange 
            _project.Setup(x => x.SearchProjectsAsync(It.IsAny<string>())).Returns(MockGetAllProjectsWithoutPictures());
            _project.Setup(x => x.GetProjectsCount()).Returns(async () => await Task.FromResult(4));

            //act
            var result = (await _controller.SearchProjects("test") as ViewResult)?.ViewData.Model as ProjectsAdministrationViewModel ?? new();
            var projects = result.Projects.ToList();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Pagination.ItemsPerPage, Is.EqualTo(6));
                Assert.That(result.Pagination.TotalItems, Is.EqualTo(4));
                Assert.That(result.Pagination.TotalPages, Is.EqualTo(1));
                Assert.That(result.Pagination.CurrentPage, Is.EqualTo(1));
            });
        }




        [Test]
        public async Task DeleteProject_ProjectIsNull_ReturnAdminInfo()
        {
            //arrange 
            Project project = default!;

            //act
            var result = await _controller.DeleteProject(project) as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/AdminInfo"));
            _project.Verify(x => x.DeleteProjectAsync(It.IsAny<Project>()), Times.Never());
        }
       

        [Test]
        public async Task DeleteProject_ProjectIsDeleted_ReturnProjectsPanel()
        {
            //arrange 
            Project project = new Project();

            //act
            var result = await _controller.DeleteProject(project) as RedirectToActionResult;

            //assert
            Assert.That(result!.ActionName, Is.EqualTo("ProjectsPanel"));
            _project.Verify(x => x.DeleteProjectAsync(It.IsAny<Project>()), Times.Once());
        }
        #endregion

        #region PostProject
        [Test]
        public async Task PostProject_ModelStateIsNotValid_ReturnAddProjectView()
        {
            //arrange
            _controller.ModelState.AddModelError("modelStateIsNotValid", "test");

            var projectAdministrationViewModel = new ProjectsAdministrationViewModel
            {
                NewProject = new Project()
            };

            //act
            var result = await _controller.PostProject(projectAdministrationViewModel) as ViewResult;
            var errorValue = result!.ViewData.ModelState["modelStateIsNotValid"];
            //assert
            Assert.That(result!.ViewName, Is.EqualTo("AddProject"));
            Assert.That(errorValue!.Errors[0].ErrorMessage, Is.EqualTo("test"));
            _project.Verify(x => x.AddProjectAsync(It.IsAny<Project>()), Times.Never());
        }


        [Test]
        public async Task PostProject_IFormFileImagesAreNull_ReturnAddProjectView()
        {
            //arrange
            var projectAdministrationViewModel = new ProjectsAdministrationViewModel
            {
                NewProject = new Project
                {
                    Title = "Test Title",
                    Description = "Test Description"
                }
            };

            //act
            var result = await _controller.PostProject(projectAdministrationViewModel) as ViewResult;


            //assert
            Assert.That(result!.ViewName, Is.EqualTo("AddProject"));
            Assert.That(result.ViewData.ModelState.ContainsKey("NoImages"), Is.EqualTo(true));
            _project.Verify(x => x.AddProjectAsync(It.IsAny<Project>()), Times.Never());
        }


        [Test]
        public async Task PostProject_ImagesAreLessThan4_ReturnAddProjectView()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();
            var mocklog = new Mock<ILogger<ProjectsAdministrationController>>();

            var controller = new ProjectsAdministrationController(mockProject.Object, mocklog.Object, _context.Object);

            var mockIFormFile1 = new Mock<IFormFile>();
            var mockIFormFile2 = new Mock<IFormFile>();
            var mockIFormFile3 = new Mock<IFormFile>();

            var fileArray = new IFormFile[]
            {
                mockIFormFile1.Object,
                mockIFormFile2.Object,
                mockIFormFile3.Object
            };

            var projectAdministrationViewModel = new ProjectsAdministrationViewModel
            {
                NewProject = new Project
                {
                    Title = "Test Title",
                    Description = "Test Description"
                },
                Files = fileArray
            };

            //act
            var result = await controller.PostProject(projectAdministrationViewModel) as ViewResult;


            //assert
            Assert.That(result!.ViewName, Is.EqualTo("AddProject"));
            Assert.That(result.ViewData.ModelState.ContainsKey("fewImages"), Is.EqualTo(true));
            mockProject.Verify(x => x.AddProjectAsync(It.IsAny<Project>()), Times.Never());
        }
        [Test]
        public async Task PostProject_ImagesAreMoreThan10_ReturnAddProjectView()
        {
            //arrange
            var mockIFormFile1 = new Mock<IFormFile>();
            var mockIFormFile2 = new Mock<IFormFile>();
            var mockIFormFile3 = new Mock<IFormFile>();
            var mockIFormFile4 = new Mock<IFormFile>();
            var mockIFormFile5 = new Mock<IFormFile>();
            var mockIFormFile6 = new Mock<IFormFile>();
            var mockIFormFile7 = new Mock<IFormFile>();
            var mockIFormFile8 = new Mock<IFormFile>();
            var mockIFormFile9 = new Mock<IFormFile>();
            var mockIFormFile10 = new Mock<IFormFile>();
            var mockIFormFile11 = new Mock<IFormFile>();

            var fileArray = new IFormFile[]
            {
                mockIFormFile1.Object,
                mockIFormFile2.Object,
                mockIFormFile3.Object,
                mockIFormFile4.Object,
                mockIFormFile5.Object,
                mockIFormFile6.Object,
                mockIFormFile7.Object,
                mockIFormFile8.Object,
                mockIFormFile9.Object,
                mockIFormFile10.Object,
                mockIFormFile11.Object
            };

            var projectAdministrationViewModel = new ProjectsAdministrationViewModel
            {
                NewProject = new Project
                {
                    Title = "Test Title",
                    Description = "Test Description"
                },
                Files = fileArray
            };

            //act
            var result = await _controller.PostProject(projectAdministrationViewModel) as ViewResult;


            //assert
            Assert.That(result!.ViewName, Is.EqualTo("AddProject"));
            Assert.That(result.ViewData.ModelState.ContainsKey("manyImages"), Is.EqualTo(true));
            _project.Verify(x => x.AddProjectAsync(It.IsAny<Project>()), Times.Never());
        }

        [Test]
        public async Task PostProject_OneFileIsNotAnImage_ReturnAddProjectView()
        {
            //arrange
            byte[] fileData = new byte[] { 200 };

            int size = 5 * 1024 * 1024;   //5mb

            var fileArray = new IFormFile[]
            {
                CreateCustomFormFileBinaryDataContent(fileData,size),
                CreateCustomFormFileBinaryDataContent(fileData,size,"image2.png","image/png"),
                CreateCustomFormFileBinaryDataContent(fileData,size,"image3.jpeg"),
                CreateCustomFormFileBinaryDataContent(fileData,size,"image4.gif","image/gif")
            };

            var projectAdministrationViewModel = new ProjectsAdministrationViewModel
            {
                NewProject = new Project
                {
                    Title = "Test Title",
                    Description = "Test Description"
                },
                Files = fileArray
            };

            //act
            var result = await _controller.PostProject(projectAdministrationViewModel) as ViewResult;


            //assert
            Assert.That(result!.ViewName, Is.EqualTo("AddProject"));
            Assert.That(result.ViewData.ModelState.ContainsKey("wrongType"), Is.EqualTo(true));
            _project.Verify(x => x.AddProjectAsync(It.IsAny<Project>()), Times.Never());
        }
        [Test]
        public async Task PostProject_OneFileIsBiggerThan10mb_ReturnAddProjectView()
        {
            //arrange
            byte[] fileData = new byte[] { 200 };

            int size = 2 * 1024 * 1024;   //2mb
            int overSize = 12 * 1024 * 1024;//12mb

            var fileArray = new IFormFile[]
            {
                CreateCustomFormFileBinaryDataContent(fileData,size),
                 CreateCustomFormFileBinaryDataContent(fileData,size,"image2.png","image/png"),
                CreateCustomFormFileBinaryDataContent(fileData,size,"image3.jpeg"),
                CreateCustomFormFileBinaryDataContent(fileData,overSize,"image4.png","image/png")
            };

            var projectAdministrationViewModel = new ProjectsAdministrationViewModel
            {
                NewProject = new Project
                {
                    Title = "Test Title",
                    Description = "Test Description"
                },
                Files = fileArray
            };

            //act
            var result = await _controller.PostProject(projectAdministrationViewModel) as ViewResult;


            //assert
            Assert.That(result!.ViewName, Is.EqualTo("AddProject"));
            Assert.That(result.ViewData.ModelState.ContainsKey("bigSize"), Is.EqualTo(true));
            _project.Verify(x => x.AddProjectAsync(It.IsAny<Project>()), Times.Never());
        }

        [Test]
        public async Task PostProject_EverythingsGoodNewProjectIsAdded_ReturnProjectsPanel()
        {
            //arrange
            byte[] fileData = new byte[] { 200 };

            int size = 2 * 1024 * 1024;   //2mb

            var fileArray = new IFormFile[]
            {
                CreateCustomFormFileBinaryDataContent(fileData,size),
                 CreateCustomFormFileBinaryDataContent(fileData,size,"image2.png","image/png"),
                CreateCustomFormFileBinaryDataContent(fileData,size,"image3.jpeg"),
                CreateCustomFormFileBinaryDataContent(fileData,size,"image4.png","image/png")
            };

            var projectAdministrationViewModel = new ProjectsAdministrationViewModel
            {
                NewProject = new Project
                {
                    Title = "Test Title",
                    Description = "Test Description"
                },
                Files = fileArray
            };

            //act
            var result = await _controller.PostProject(projectAdministrationViewModel) as RedirectToActionResult;


            //assert
            Assert.That(result!.ActionName, Is.EqualTo("ProjectsPanel"));
            _project.Verify(x => x.AddProjectAsync(It.IsAny<Project>()), Times.Once());
        }
        #endregion

        #region EditProjectPost
        [Test]
        public async Task EditProjectPost_ModelStateIsNotValid_ReturnView()
        {
            //arrange
            var project = new Project
            {
                Id = 1,
                Title = "TestTitle",
                Description = "TestDescription",
                Pictures = GetMockPictures()
            };
            _project.Setup(x => x.GetProjectIncludePictures(It.IsAny<int>())).ReturnsAsync(project);
            _controller.ModelState.AddModelError("modelStateIsNotValid", "test");

            var projectAdministrationViewModel = new ProjectsAdministrationViewModel
            {
                NewProject = new Project()
                {
                    Id = 1
                }
            };

            //act
            var result = await _controller.EditProjectPost(projectAdministrationViewModel) as ViewResult;

            var model = result!.ViewData.Model as ProjectsAdministrationViewModel;

            var errorValue = result!.ViewData.ModelState["modelStateIsNotValid"];
            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result!.ViewName, Is.EqualTo("EditProject"));
                Assert.That(model?.NewProject.Id, Is.EqualTo(1));
                Assert.That(model?.NewProject!.Pictures![0].Id, Is.EqualTo(1));
                Assert.That(model?.NewProject!.Pictures![0].ImageName, Is.EqualTo("image1.jpeg"));
                Assert.That(model?.NewProject!.Pictures![0].ContentType, Is.EqualTo("image/jpeg"));
                Assert.That(model?.NewProject!.Pictures![0].Extension, Is.EqualTo("jpeg"));

                Assert.That(model?.NewProject!.Pictures![3].Id, Is.EqualTo(4));
                Assert.That(model?.NewProject!.Pictures![3].ImageName, Is.EqualTo("image4.png"));
                Assert.That(model?.NewProject!.Pictures![3].ContentType, Is.EqualTo("image/png"));
                Assert.That(model?.NewProject!.Pictures![3].Extension, Is.EqualTo("png"));

                Assert.That(errorValue!.Errors[0].ErrorMessage, Is.EqualTo("test"));
            });
            _project.Verify(x => x.UpdateProject(It.IsAny<Project>()), Times.Never());
        }

        [Test]
        public async Task EditProjectPost_NoImages_ReturnView()
        {
            //arrange
            var project = new Project
            {
                Id = 1,
                Title = "TestTitle",
                Description = "TestDescription",
                Pictures = GetMockPictures()
            };
            _project.Setup(x => x.GetProjectIncludePictures(It.IsAny<int>())).ReturnsAsync(project);

            var projectAdministrationViewModel = new ProjectsAdministrationViewModel
            {
                NewProject = new Project()
                {
                    Id = 1,
                    Title = "Test Title",
                    Description = "Test Description"
                }
            };

            //act
            var result = await _controller.EditProjectPost(projectAdministrationViewModel) as ViewResult;

            var model = result!.ViewData.Model as ProjectsAdministrationViewModel;

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result!.ViewName, Is.EqualTo("EditProject"));
                Assert.That(model?.NewProject.Id, Is.EqualTo(1));
                Assert.That(model?.NewProject!.Pictures![0].Id, Is.EqualTo(1));
                Assert.That(model?.NewProject!.Pictures![0].ImageName, Is.EqualTo("image1.jpeg"));
                Assert.That(model?.NewProject!.Pictures![0].ContentType, Is.EqualTo("image/jpeg"));
                Assert.That(model?.NewProject!.Pictures![0].Extension, Is.EqualTo("jpeg"));

                Assert.That(model?.NewProject!.Pictures![3].Id, Is.EqualTo(4));
                Assert.That(model?.NewProject!.Pictures![3].ImageName, Is.EqualTo("image4.png"));
                Assert.That(model?.NewProject!.Pictures![3].ContentType, Is.EqualTo("image/png"));
                Assert.That(model?.NewProject!.Pictures![3].Extension, Is.EqualTo("png"));

                Assert.That(result.ViewData.ModelState.ContainsKey("NoImages"), Is.True);
            });
            _project.Verify(x => x.UpdateProject(It.IsAny<Project>()), Times.Never());
        }

        [Test]
        public async Task EditProjectPost_NotEnoughImages_ReturnView()
        {
            //arrange
            var project = new Project
            {
                Id = 1,
                Title = "TestTitle",
                Description = "TestDescription",
                Pictures = GetMockPictures()
            };
            _project.Setup(x => x.GetProjectIncludePictures(It.IsAny<int>())).ReturnsAsync(project);

            byte[] fileData = new byte[] { 200 };

            int size = 2 * 1024 * 1024;   //2mb

            var fileArray = new IFormFile[]
            {
                 CreateCustomFormFileBinaryDataContent(fileData,size),
                 CreateCustomFormFileBinaryDataContent(fileData,size,"image2.png","image/png"),
                CreateCustomFormFileBinaryDataContent(fileData,size,"image3.png","image/png")
            };

            var projectAdministrationViewModel = new ProjectsAdministrationViewModel
            {
                NewProject = new Project()
                {
                    Id = 1,
                    Title = "Test Title",
                    Description = "Test Description"
                },
                Files = fileArray
            };

            //act
            var result = await _controller.EditProjectPost(projectAdministrationViewModel) as ViewResult;

            var model = result!.ViewData.Model as ProjectsAdministrationViewModel;

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result!.ViewName, Is.EqualTo("EditProject"));
                Assert.That(model?.NewProject.Id, Is.EqualTo(1));
                Assert.That(model?.NewProject!.Pictures![0].Id, Is.EqualTo(1));
                Assert.That(model?.NewProject!.Pictures![0].ImageName, Is.EqualTo("image1.jpeg"));
                Assert.That(model?.NewProject!.Pictures![0].ContentType, Is.EqualTo("image/jpeg"));
                Assert.That(model?.NewProject!.Pictures![0].Extension, Is.EqualTo("jpeg"));

                Assert.That(model?.NewProject!.Pictures![3].Id, Is.EqualTo(4));
                Assert.That(model?.NewProject!.Pictures![3].ImageName, Is.EqualTo("image4.png"));
                Assert.That(model?.NewProject!.Pictures![3].ContentType, Is.EqualTo("image/png"));
                Assert.That(model?.NewProject!.Pictures![3].Extension, Is.EqualTo("png"));

                Assert.That(result.ViewData.ModelState.ContainsKey("fewImages"), Is.True);
            });
            _project.Verify(x => x.UpdateProject(It.IsAny<Project>()), Times.Never());
        }

        [Test]
        public async Task EditProjectPost_ToManyImages_ReturnView()
        {
            //arrange
            var project = new Project
            {
                Id = 1,
                Title = "TestTitle",
                Description = "TestDescription",
                Pictures = GetMockPictures()
            };
            _project.Setup(x => x.GetProjectIncludePictures(It.IsAny<int>())).ReturnsAsync(project);

            byte[] fileData = new byte[] { 200 };

            int size = 2 * 1024 * 1024;   //2mb

            var fileArray = new IFormFile[]
            {
                 CreateCustomFormFileBinaryDataContent(fileData,size),
                 CreateCustomFormFileBinaryDataContent(fileData,size,"image2.png","image/png"),
                CreateCustomFormFileBinaryDataContent(fileData,size,"image3.png","image/png"),
                 CreateCustomFormFileBinaryDataContent(fileData,size,"image4.png","image/png"),
                  CreateCustomFormFileBinaryDataContent(fileData,size,"image5.jpeg","image/jpeg"),
                   CreateCustomFormFileBinaryDataContent(fileData,size,"image6.png","image/png"),
                  CreateCustomFormFileBinaryDataContent(fileData,size,"image7.jpeg","image/jpeg"),
                   CreateCustomFormFileBinaryDataContent(fileData,size,"image8.png","image/png"),
                  CreateCustomFormFileBinaryDataContent(fileData,size,"image9.jpeg","image/jpeg"),
                   CreateCustomFormFileBinaryDataContent(fileData,size,"image10.png","image/png"),
                  CreateCustomFormFileBinaryDataContent(fileData,size,"image11.jpeg","image/jpeg")
            };

            var projectAdministrationViewModel = new ProjectsAdministrationViewModel
            {
                NewProject = new Project()
                {
                    Id = 1,
                    Title = "Test Title",
                    Description = "Test Description"
                },
                Files = fileArray
            };

            //act
            var result = await _controller.EditProjectPost(projectAdministrationViewModel) as ViewResult;

            var model = result!.ViewData.Model as ProjectsAdministrationViewModel;

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result!.ViewName, Is.EqualTo("EditProject"));
                Assert.That(model?.NewProject.Id, Is.EqualTo(1));
                Assert.That(model?.NewProject!.Pictures![0].Id, Is.EqualTo(1));
                Assert.That(model?.NewProject!.Pictures![0].ImageName, Is.EqualTo("image1.jpeg"));
                Assert.That(model?.NewProject!.Pictures![0].ContentType, Is.EqualTo("image/jpeg"));
                Assert.That(model?.NewProject!.Pictures![0].Extension, Is.EqualTo("jpeg"));

                Assert.That(model?.NewProject!.Pictures![3].Id, Is.EqualTo(4));
                Assert.That(model?.NewProject!.Pictures![3].ImageName, Is.EqualTo("image4.png"));
                Assert.That(model?.NewProject!.Pictures![3].ContentType, Is.EqualTo("image/png"));
                Assert.That(model?.NewProject!.Pictures![3].Extension, Is.EqualTo("png"));

                Assert.That(result.ViewData.ModelState.ContainsKey("manyImages"), Is.True);
            });
            _project.Verify(x => x.UpdateProject(It.IsAny<Project>()), Times.Never());
        }

        [Test]
        public async Task EditProjectPost_OneFileIsNotPNGorJPEG_ReturnView()
        {
            //arrange
            var project = new Project
            {
                Id = 1,
                Title = "TestTitle",
                Description = "TestDescription",
                Pictures = GetMockPictures()
            };
            _project.Setup(x => x.GetProjectIncludePictures(It.IsAny<int>())).ReturnsAsync(project);

            byte[] fileData = new byte[] { 200 };

            int size = 2 * 1024 * 1024;   //2mb

            var fileArray = new IFormFile[]
            {
                 CreateCustomFormFileBinaryDataContent(fileData,size),
                 CreateCustomFormFileBinaryDataContent(fileData,size,"image2.png","image/png"),
                CreateCustomFormFileBinaryDataContent(fileData,size,"image3.png","image/png"),
                CreateCustomFormFileBinaryDataContent(fileData,size,"image4.gif","image/gif")//set as gif
            };

            var projectAdministrationViewModel = new ProjectsAdministrationViewModel
            {
                NewProject = new Project()
                {
                    Id = 1,
                    Title = "Test Title",
                    Description = "Test Description"
                },
                Files = fileArray
            };

            //act
            var result = await _controller.EditProjectPost(projectAdministrationViewModel) as ViewResult;

            var model = result!.ViewData.Model as ProjectsAdministrationViewModel;

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result!.ViewName, Is.EqualTo("EditProject"));
                Assert.That(model?.NewProject.Id, Is.EqualTo(1));
                Assert.That(model?.NewProject!.Pictures![0].Id, Is.EqualTo(1));
                Assert.That(model?.NewProject!.Pictures![0].ImageName, Is.EqualTo("image1.jpeg"));
                Assert.That(model?.NewProject!.Pictures![0].ContentType, Is.EqualTo("image/jpeg"));
                Assert.That(model?.NewProject!.Pictures![0].Extension, Is.EqualTo("jpeg"));

                Assert.That(model?.NewProject!.Pictures![3].Id, Is.EqualTo(4));
                Assert.That(model?.NewProject!.Pictures![3].ImageName, Is.EqualTo("image4.png"));
                Assert.That(model?.NewProject!.Pictures![3].ContentType, Is.EqualTo("image/png"));
                Assert.That(model?.NewProject!.Pictures![3].Extension, Is.EqualTo("png"));

                Assert.That(result.ViewData.ModelState.ContainsKey("wrongType"), Is.True);
            });
            _project.Verify(x => x.UpdateProject(It.IsAny<Project>()), Times.Never());
        }

        [Test]
        public async Task EditProjectPost_OneFileIsBIggerThan10mb_ReturnView()
        {
            //arrange
            var project = new Project
            {
                Id = 1,
                Title = "TestTitle",
                Description = "TestDescription",
                Pictures = GetMockPictures()
            };
            _project.Setup(x => x.GetProjectIncludePictures(It.IsAny<int>())).ReturnsAsync(project);

            byte[] fileData = new byte[] { 200 };

            int size = 2 * 1024 * 1024;   //2mb
            int overSize = 12 * 1024 * 1024;//12mb

            var fileArray = new IFormFile[]
            {
                 CreateCustomFormFileBinaryDataContent(fileData,size),
                 CreateCustomFormFileBinaryDataContent(fileData,size,"image2.png","image/png"),
                CreateCustomFormFileBinaryDataContent(fileData,size,"image3.png","image/png"),
                CreateCustomFormFileBinaryDataContent(fileData,overSize,"image4.png","image/png")
            };

            var projectAdministrationViewModel = new ProjectsAdministrationViewModel
            {
                NewProject = new Project()
                {
                    Id = 1,
                    Title = "Test Title",
                    Description = "Test Description"
                },
                Files = fileArray
            };

            //act
            var result = await _controller.EditProjectPost(projectAdministrationViewModel) as ViewResult;

            var model = result!.ViewData.Model as ProjectsAdministrationViewModel;

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result!.ViewName, Is.EqualTo("EditProject"));
                Assert.That(model?.NewProject.Id, Is.EqualTo(1));
                Assert.That(model?.NewProject!.Pictures![0].Id, Is.EqualTo(1));
                Assert.That(model?.NewProject!.Pictures![0].ImageName, Is.EqualTo("image1.jpeg"));
                Assert.That(model?.NewProject!.Pictures![0].ContentType, Is.EqualTo("image/jpeg"));
                Assert.That(model?.NewProject!.Pictures![0].Extension, Is.EqualTo("jpeg"));

                Assert.That(model?.NewProject!.Pictures![3].Id, Is.EqualTo(4));
                Assert.That(model?.NewProject!.Pictures![3].ImageName, Is.EqualTo("image4.png"));
                Assert.That(model?.NewProject!.Pictures![3].ContentType, Is.EqualTo("image/png"));
                Assert.That(model?.NewProject!.Pictures![3].Extension, Is.EqualTo("png"));

                Assert.That(result.ViewData.ModelState.ContainsKey("bigSize"), Is.True);
            });
            _project.Verify(x => x.UpdateProject(It.IsAny<Project>()), Times.Never());
        }

        [Test]
        public async Task EditProjectPost_CouldNotDeleteTheOldPicturesForUnknownReasons_ReturnAdminInfoPage()
        {
            //arrange
            var project = new Project
            {
                Id = 1,
                Title = "TestTitle",
                Description = "TestDescription",
                Pictures = GetMockPictures()
            };
            _project.Setup(x => x.GetProjectIncludePictures(It.IsAny<int>())).ReturnsAsync(project);
            _project.Setup(x => x.DeleteAllPicturesForSpecificProject(It.IsAny<int>())).Returns(async () => await Task.FromResult(false));

            byte[] fileData = new byte[] { 200 };

            int size = 2 * 1024 * 1024;   //2mb

            var fileArray = new IFormFile[]
            {
                 CreateCustomFormFileBinaryDataContent(fileData,size),
                 CreateCustomFormFileBinaryDataContent(fileData,size,"image2.png","image/png"),
                CreateCustomFormFileBinaryDataContent(fileData,size,"image3.png","image/png"),
                CreateCustomFormFileBinaryDataContent(fileData,size,"image4.png","image/png")
            };

            var projectAdministrationViewModel = new ProjectsAdministrationViewModel
            {
                NewProject = new Project()
                {
                    Id = 1,
                    Title = "Test Title",
                    Description = "Test Description"
                },
                Files = fileArray
            };

            //act
            var result = await _controller.EditProjectPost(projectAdministrationViewModel) as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/AdminInfo"));
            _project.Verify(x => x.DeleteAllPicturesForSpecificProject(It.IsAny<int>()), Times.Once());
            _project.Verify(x => x.UpdateProject(It.IsAny<Project>()), Times.Never());
        }

        [Test]
        public async Task EditProjectPost_EverythingWorksProjectIsEdited_ReturnAdminInfoPage()
        {
            //arrange
            var project = new Project
            {
                Id = 1,
                Title = "TestTitle",
                Description = "TestDescription",
                Pictures = GetMockPictures()
            };
            _project.Setup(x => x.GetProjectIncludePictures(It.IsAny<int>())).ReturnsAsync(project);
            _project.Setup(x => x.DeleteAllPicturesForSpecificProject(It.IsAny<int>())).Returns(async () => await Task.FromResult(true));
            _project.Setup(x => x.UpdateProject(It.IsAny<Project>())).Returns(async () => await Task.FromResult(true));

            byte[] fileData = new byte[] { 200 };

            int size = 2 * 1024 * 1024;   //2mb

            var fileArray = new IFormFile[]
            {
                 CreateCustomFormFileBinaryDataContent(fileData,size),
                 CreateCustomFormFileBinaryDataContent(fileData,size,"image2.png","image/png"),
                CreateCustomFormFileBinaryDataContent(fileData,size,"image3.png","image/png"),
                CreateCustomFormFileBinaryDataContent(fileData,size,"image4.png","image/png")
            };

            var projectAdministrationViewModel = new ProjectsAdministrationViewModel
            {
                NewProject = new Project()
                {
                    Id = 1,
                    Title = "Test Title",
                    Description = "Test Description"
                },
                Files = fileArray
            };

            //act
            var result = await _controller.EditProjectPost(projectAdministrationViewModel) as RedirectToActionResult;

            //assert
            Assert.That(result!.ActionName, Is.EqualTo("ProjectsPanel"));
            _project.Verify(x => x.DeleteAllPicturesForSpecificProject(It.IsAny<int>()), Times.Once());
            _project.Verify(x => x.UpdateProject(It.IsAny<Project>()), Times.Once());
        }
        #endregion

        #region EditProject
        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        public async Task EditProject_ProjectIdIsLessThanZeroOrEqual_ReturnProjectsPanel(int projectId)
        {
            //act
            var result = await _controller.EditProject(projectId) as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("ProjectsPanel"));
            _project.Verify(x => x.GetProjectIncludePictures(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public async Task EditProject_ProjectIsNotFound_ReturnAdminInfo()
        {
            //arrange 
            Project project = default!;
            _project.Setup(x => x.GetProjectIncludePictures(It.IsAny<int>())).ReturnsAsync(project);

            //act
            var result = await _controller.EditProject(1) as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/AdminInfo"));
        }

        [Test]
        public async Task EditProject_ProjectIsNotNull_ReturnEditProjectView()
        {
            //arrange 
            Project project = new Project()
            {
                Id = 1,
                Title = "Test Title",
                Description = "Test Description",
                Pictures = GetMockPictures()
            };
            _project.Setup(x => x.GetProjectIncludePictures(It.IsAny<int>())).ReturnsAsync(project);
          
            //act
            var result = await _controller.EditProject(project.Id) as ViewResult;
            var model = result!.ViewData.Model as ProjectsAdministrationViewModel ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(model.NewProject.Id, Is.EqualTo(1));
                Assert.That(model.NewProject.Title, Is.EqualTo("Test Title"));
                Assert.That(model.NewProject.Description, Is.EqualTo("Test Description"));

                Assert.That(model?.NewProject.Id, Is.EqualTo(1));
                Assert.That(model?.NewProject!.Pictures![0].Id, Is.EqualTo(1));
                Assert.That(model?.NewProject!.Pictures![0].ImageName, Is.EqualTo("image1.jpeg"));
                Assert.That(model?.NewProject!.Pictures![0].ContentType, Is.EqualTo("image/jpeg"));
                Assert.That(model?.NewProject!.Pictures![0].Extension, Is.EqualTo("jpeg"));

                Assert.That(model?.NewProject!.Pictures![3].Id, Is.EqualTo(4));
                Assert.That(model?.NewProject!.Pictures![3].ImageName, Is.EqualTo("image4.png"));
                Assert.That(model?.NewProject!.Pictures![3].ContentType, Is.EqualTo("image/png"));
                Assert.That(model?.NewProject!.Pictures![3].Extension, Is.EqualTo("png"));
            });

            _project.Verify(x => x.GetProjectIncludePictures(It.IsAny<int>()), Times.Once());
        }
        #endregion
    }
}
