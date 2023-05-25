using IPDImexWebsite.Controllers;
using IPDImexWebsite.Models;
using IPDImexWebsite.Models.Repository;
using IPDImexWebsite.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;


namespace IPDImexWebsiteUnitTests
{
    [TestFixture]
    internal class ProjectsAdministrationTests
    {
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

            foreach(var project in projects)
            {
                yield return project;
            }
            await Task.CompletedTask;
        }

        public async IAsyncEnumerable<Picture> MockGetAllPicturesOfSpecificProjects()
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
            foreach (var picture in pictures)
            {
                yield return picture;
            }
            await Task.CompletedTask;
        }


        public IFormFile CreateCustomFormFileBinaryDataContent(byte[] imageData, int length = 1 * 1024 * 1024, string fileName = "image1.jpeg", string contentType = "image/jpeg", string name="file")
        {
            var stream = new MemoryStream(imageData);

            IFormFile formFile = new FormFile(stream, 0, (long)length, name, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };

            return formFile;
        }


        #region region ProjectPanel, SearchProjects, DeleteProjects
        [Test]
        public async Task ProjectPanel_CanGetAllProjectsWithoutPicturesAndCountThem_ReturnProjectPanelView()
        {
            //arrange 
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.GetAllProjectsWithoutPictures()).Returns(MockGetAllProjectsWithoutPictures());
            mockProject.Setup(x => x.GetProjectsCount()).Returns(async () => await Task.FromResult(4));

            var controller = new ProjectsAdministrationController(mockProject.Object);

            //act
            var result = (await  controller.ProjectsPanel() as ViewResult)?.ViewData.Model as ProjectsAdministrationViewModel ?? new();
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
        public async Task SearchProjects_SearchCruteriaIsNull_ReturnToActionProjectsPanel()
        {
            //arrange 
            var mockProject = new Mock<IRepositoryProject>();
            var controller = new ProjectsAdministrationController(mockProject.Object);

            //act
            var result = await controller.SearchProjects() as RedirectToActionResult;

            //assert
            Assert.That(result!.ActionName, Is.EqualTo("ProjectsPanel"));
        }

        [Test]
        public async Task SearchProject_SearchCriteriaIsNull_ReturnView()
        {
            //arrange 
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.SearchProjectsAsync(It.IsAny<string>())).Returns(MockGetAllProjectsWithoutPictures());
            mockProject.Setup(x => x.GetProjectsCount()).Returns(async () => await Task.FromResult(4));

            var controller = new ProjectsAdministrationController(mockProject.Object);

            //act
            var result = (await controller.SearchProjects("test") as ViewResult)?.ViewData.Model as ProjectsAdministrationViewModel ?? new();
            var projects = result.Projects.ToList();
            //assert
            Assert.Multiple(() =>
            {
                Assert.That(projects, Has.Count.EqualTo(4));
                Assert.That(projects[0].Id, Is.EqualTo(4));
                Assert.That(projects[0].Description, Is.EqualTo("Gradinita cu vaci"));

                Assert.That(result.TotalProjects, Is.EqualTo(4));

                Assert.That(result.SearchCriteria, Is.EqualTo("test"));
                Assert.That(result.SearchRequest, Is.True);
            });
        }

        [Test]
        public async Task DeleteProject_ProjectIsNull_ReturnAdminInfo()
        {
            //arrange 
            var mockProject = new Mock<IRepositoryProject>();
            var controller = new ProjectsAdministrationController(mockProject.Object);
            Project project = default!;

            //act
           var result = await controller.DeleteProject(project) as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/AdminInfo"));
            mockProject.Verify(x => x.DeleteProjectAsync(It.IsAny<Project>()), Times.Never());
        }

        [Test]
        public async Task DeleteProject_ProjectIsNotNull_ButCouldNotBeDeletedForUnknownReason_ReturnAdminInfo()
        {
            //arrange 
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.DeleteProjectAsync(It.IsAny<Project>())).Returns(async () => await Task.FromResult(false));
            var controller = new ProjectsAdministrationController(mockProject.Object);
            Project project = new Project();

            //act
            var result = await controller.DeleteProject(project) as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/AdminInfo"));
            mockProject.Verify(x => x.DeleteProjectAsync(It.IsAny<Project>()), Times.Once());
        }

        [Test]
        public async Task DeleteProject_ProjectIsNotNullAndItWasDeleted_ReturnProjectsPanel()
        {
            //arrange 
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.DeleteProjectAsync(It.IsAny<Project>())).Returns(async () => await Task.FromResult(true));
            var controller = new ProjectsAdministrationController(mockProject.Object);
            Project project = new Project();

            //act
            var result = await controller.DeleteProject(project) as RedirectToActionResult;

            //assert
            Assert.That(result!.ActionName, Is.EqualTo("ProjectsPanel"));
        }
        #endregion

        #region PostProject
        [Test]
        public async Task PostProject_ModelStateIsNotValid_ReturnAddProjectView()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();
            var controller = new ProjectsAdministrationController(mockProject.Object);
            controller.ModelState.AddModelError("modelStateIsNotValid", "test");
           
            var projectAdministrationViewModel = new ProjectsAdministrationViewModel
            {
                NewProject = new Project()
            };

            //act
            var result =  await controller.PostProject(projectAdministrationViewModel) as ViewResult;
            var errorValue = result!.ViewData.ModelState["modelStateIsNotValid"];
            //assert
            Assert.That(result!.ViewName, Is.EqualTo("AddProject"));
            Assert.That(errorValue!.Errors[0].ErrorMessage, Is.EqualTo("test"));
            mockProject.Verify(x => x.AddProjectAsync(It.IsAny<Project>()), Times.Never());
        }

      
        [Test]
        public async Task PostProject_IFormFileImagesAreNull_ReturnAddProjectView()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();
            
            var controller = new ProjectsAdministrationController(mockProject.Object);


            var projectAdministrationViewModel = new ProjectsAdministrationViewModel
            {
                NewProject = new Project
                {
                    Title = "Test Title",
                    Description = "Test Description"
                }
            };

            //act
            var result = await controller.PostProject(projectAdministrationViewModel) as ViewResult;
           

            //assert
            Assert.That(result!.ViewName, Is.EqualTo("AddProject"));
            Assert.That(result.ViewData.ModelState.ContainsKey("NoImages"), Is.EqualTo(true));
            mockProject.Verify(x => x.AddProjectAsync(It.IsAny<Project>()), Times.Never());
        }


        [Test]
        public async Task PostProject_ImagesAreLessThan4_ReturnAddProjectView()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();

            var controller = new ProjectsAdministrationController(mockProject.Object);

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
            var mockProject = new Mock<IRepositoryProject>();

            var controller = new ProjectsAdministrationController(mockProject.Object);

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
            var result = await controller.PostProject(projectAdministrationViewModel) as ViewResult;


            //assert
            Assert.That(result!.ViewName, Is.EqualTo("AddProject"));
            Assert.That(result.ViewData.ModelState.ContainsKey("manyImages"), Is.EqualTo(true));
            mockProject.Verify(x => x.AddProjectAsync(It.IsAny<Project>()), Times.Never());
        }

        [Test]
        public async Task PostProject_OneFileIsNotAnImage_ReturnAddProjectView()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();

            var controller = new ProjectsAdministrationController(mockProject.Object);

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
            var result = await controller.PostProject(projectAdministrationViewModel) as ViewResult;


            //assert
            Assert.That(result!.ViewName, Is.EqualTo("AddProject"));
            Assert.That(result.ViewData.ModelState.ContainsKey("wrongType"), Is.EqualTo(true));
            mockProject.Verify(x => x.AddProjectAsync(It.IsAny<Project>()), Times.Never());
        }
        [Test]
        public async Task PostProject_OneFileIsBiggerThan10mb_ReturnAddProjectView()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();

            var controller = new ProjectsAdministrationController(mockProject.Object);


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
            var result = await controller.PostProject(projectAdministrationViewModel) as ViewResult;


            //assert
            Assert.That(result!.ViewName, Is.EqualTo("AddProject"));
            Assert.That(result.ViewData.ModelState.ContainsKey("bigSize"), Is.EqualTo(true));
            mockProject.Verify(x => x.AddProjectAsync(It.IsAny<Project>()), Times.Never());
        }

        [Test]
        public async Task PostProject_EverythingsGoodNewProjectIsAdded_ReturnProjectsPanel()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.AddProjectAsync(It.IsAny<Project>())).Returns(async () => await Task.FromResult(true));

            var controller = new ProjectsAdministrationController(mockProject.Object);


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
            var result = await controller.PostProject(projectAdministrationViewModel) as RedirectToActionResult;


            //assert
            Assert.That(result!.ActionName, Is.EqualTo("ProjectsPanel"));
            mockProject.Verify(x => x.AddProjectAsync(It.IsAny<Project>()), Times.Once());
        }
        [Test]
        public async Task PostProject_ProjectCouldNotBeAddedForUnknownReasons_ReturnAdminInfo()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.AddProjectAsync(It.IsAny<Project>())).Returns(async () => await Task.FromResult(false));

            var controller = new ProjectsAdministrationController(mockProject.Object);


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
            var result = await controller.PostProject(projectAdministrationViewModel) as RedirectToPageResult;


            //assert
            Assert.That(result!.PageName, Is.EqualTo("/AdminInfo"));
            mockProject.Verify(x => x.AddProjectAsync(It.IsAny<Project>()), Times.Once());
        }
        [Test]
        public async Task PostProject_TryingToAddTrowsAnException_ReturnAdminInfo()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.AddProjectAsync(It.IsAny<Project>())).Throws(new Exception("An error occurred while adding the project"));

            var controller = new ProjectsAdministrationController(mockProject.Object);


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
            var result = await controller.PostProject(projectAdministrationViewModel) as RedirectToPageResult;


            //assert
            Assert.That(result!.PageName, Is.EqualTo("/AdminInfo"));
            Assert.That(result.RouteValues!["info"], Is.EqualTo("Ceva nu a mers bine, încearcă dinou sau contactează dezvoltatorul!"));
            mockProject.Verify(x => x.AddProjectAsync(It.IsAny<Project>()), Times.Once());
            Assert.ThrowsAsync<Exception>(async () => await mockProject.Object.AddProjectAsync(It.IsAny<Project>()));

        }
        #endregion

        #region EditProjectPost
        [Test]
        public async Task EditProjectPost_ModelStateIsNotValid_ReturnView()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.GetAllPicturesOfSpecificProject(It.IsAny<int>())).Returns(MockGetAllPicturesOfSpecificProjects());
         
            var controller = new ProjectsAdministrationController(mockProject.Object);
            controller.ModelState.AddModelError("modelStateIsNotValid", "test");

            var projectAdministrationViewModel = new ProjectsAdministrationViewModel
            {
                NewProject = new Project()
                {
                    Id = 1
                }
            };

            //act
            var result = await controller.EditProjectPost(projectAdministrationViewModel) as ViewResult;

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
            mockProject.Verify(x => x.EditProject(It.IsAny<Project>()), Times.Never());
        }

        [Test]
        public async Task EditProjectPost_NoImages_ReturnView()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.GetAllPicturesOfSpecificProject(It.IsAny<int>())).Returns(MockGetAllPicturesOfSpecificProjects());

            var controller = new ProjectsAdministrationController(mockProject.Object);

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
            var result = await controller.EditProjectPost(projectAdministrationViewModel) as ViewResult;

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
            mockProject.Verify(x => x.EditProject(It.IsAny<Project>()), Times.Never());
        }

        [Test]
        public async Task EditProjectPost_NotEnoughImages_ReturnView()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.GetAllPicturesOfSpecificProject(It.IsAny<int>())).Returns(MockGetAllPicturesOfSpecificProjects());

            var controller = new ProjectsAdministrationController(mockProject.Object);

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
            var result = await controller.EditProjectPost(projectAdministrationViewModel) as ViewResult;

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
            mockProject.Verify(x => x.EditProject(It.IsAny<Project>()), Times.Never());
        }

        [Test]
        public async Task EditProjectPost_ToManyImages_ReturnView()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.GetAllPicturesOfSpecificProject(It.IsAny<int>())).Returns(MockGetAllPicturesOfSpecificProjects());

            var controller = new ProjectsAdministrationController(mockProject.Object);

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
            var result = await controller.EditProjectPost(projectAdministrationViewModel) as ViewResult;

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
            mockProject.Verify(x => x.EditProject(It.IsAny<Project>()), Times.Never());
        }

        [Test]
        public async Task EditProjectPost_OneFileIsNotPNGorJPEG_ReturnView()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.GetAllPicturesOfSpecificProject(It.IsAny<int>())).Returns(MockGetAllPicturesOfSpecificProjects());

            var controller = new ProjectsAdministrationController(mockProject.Object);

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
            var result = await controller.EditProjectPost(projectAdministrationViewModel) as ViewResult;

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
            mockProject.Verify(x => x.EditProject(It.IsAny<Project>()), Times.Never());
        }

        [Test]
        public async Task EditProjectPost_OneFileIsBIggerThan10mb_ReturnView()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.GetAllPicturesOfSpecificProject(It.IsAny<int>())).Returns(MockGetAllPicturesOfSpecificProjects());

            var controller = new ProjectsAdministrationController(mockProject.Object);

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
            var result = await controller.EditProjectPost(projectAdministrationViewModel) as ViewResult;

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
            mockProject.Verify(x => x.EditProject(It.IsAny<Project>()), Times.Never());
        }

        [Test]
        public async Task EditProjectPost_CouldNotDeleteTheOldPicturesForUnknownReasons_ReturnAdminInfoPage()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.GetAllPicturesOfSpecificProject(It.IsAny<int>())).Returns(MockGetAllPicturesOfSpecificProjects());
            mockProject.Setup(x => x.DeleteAllPicturesForSpecificProject(It.IsAny<int>())).Returns(async () => await Task.FromResult(false));

            var controller = new ProjectsAdministrationController(mockProject.Object);

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
            var result = await controller.EditProjectPost(projectAdministrationViewModel) as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/AdminInfo"));
            mockProject.Verify(x => x.DeleteAllPicturesForSpecificProject(It.IsAny<int>()), Times.Once());
            mockProject.Verify(x => x.EditProject(It.IsAny<Project>()), Times.Never());
        }


        [Test]
        public async Task EditProjectPost_CanDeleteTheOldPicturesButCouldNotEditTheProject_ReturnAdminInfoPage()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.GetAllPicturesOfSpecificProject(It.IsAny<int>())).Returns(MockGetAllPicturesOfSpecificProjects());
            mockProject.Setup(x => x.DeleteAllPicturesForSpecificProject(It.IsAny<int>())).Returns(async () => await Task.FromResult(true));
            mockProject.Setup(x => x.EditProject(It.IsAny<Project>())).Returns(async () => await Task.FromResult(false));

            var controller = new ProjectsAdministrationController(mockProject.Object);

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
            var result = await controller.EditProjectPost(projectAdministrationViewModel) as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/AdminInfo"));
            mockProject.Verify(x => x.DeleteAllPicturesForSpecificProject(It.IsAny<int>()), Times.Once());
            mockProject.Verify(x => x.EditProject(It.IsAny<Project>()), Times.Once());
        }

        [Test]
        public async Task EditProjectPost_EverythingWorksProjectIsEdited_ReturnAdminInfoPage()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.GetAllPicturesOfSpecificProject(It.IsAny<int>())).Returns(MockGetAllPicturesOfSpecificProjects());
            mockProject.Setup(x => x.DeleteAllPicturesForSpecificProject(It.IsAny<int>())).Returns(async () => await Task.FromResult(true));
            mockProject.Setup(x => x.EditProject(It.IsAny<Project>())).Returns(async () => await Task.FromResult(true));

            var controller = new ProjectsAdministrationController(mockProject.Object);

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
            var result = await controller.EditProjectPost(projectAdministrationViewModel) as RedirectToActionResult;

            //assert
            Assert.That(result!.ActionName, Is.EqualTo("ProjectsPanel"));
            mockProject.Verify(x => x.DeleteAllPicturesForSpecificProject(It.IsAny<int>()), Times.Once());
            mockProject.Verify(x => x.EditProject(It.IsAny<Project>()), Times.Once());
        }

        [Test]
        public async Task EditProjectPost_ThrowsException_ReturnAdminInfoPage()
        {
            //arrange
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.GetAllPicturesOfSpecificProject(It.IsAny<int>())).Returns(MockGetAllPicturesOfSpecificProjects());
            mockProject.Setup(x => x.DeleteAllPicturesForSpecificProject(It.IsAny<int>())).Returns(async () => await Task.FromResult(true));
            mockProject.Setup(x => x.EditProject(It.IsAny<Project>())).Throws(new Exception("An error ocurreed while editing the project"));

            var controller = new ProjectsAdministrationController(mockProject.Object);

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
            var result = await controller.EditProjectPost(projectAdministrationViewModel) as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/AdminInfo"));
            mockProject.Verify(x => x.DeleteAllPicturesForSpecificProject(It.IsAny<int>()), Times.Once());
            mockProject.Verify(x => x.EditProject(It.IsAny<Project>()), Times.Once());

            Assert.That(result.RouteValues!["info"], Is.EqualTo("Ceva nu a mers bine, încearcă dinou sau contactează dezvoltatorul!"));
            Assert.ThrowsAsync<Exception>(async () => await mockProject.Object.EditProject(It.IsAny<Project>()));
        }


        #endregion

        #region EditProject
        [Test]
        public async Task EditProject_ProjectToEditIsNull_ReturnAdminInfo()
        {
            //arrange 
            var mockProject = new Mock<IRepositoryProject>();
            var controller = new ProjectsAdministrationController(mockProject.Object);

            Project project = default!;

            //act
            var result = await controller.EditProject(project!) as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/AdminInfo"));
        }
        [Test]
        public async Task EditProject_ProjectIsNotNull_ReturnEditProjectView()
        {
            //arrange 
            var mockProject = new Mock<IRepositoryProject>();
            mockProject.Setup(x => x.GetAllPicturesOfSpecificProject(It.IsAny<int>())).Returns(MockGetAllPicturesOfSpecificProjects());

            var controller = new ProjectsAdministrationController(mockProject.Object);

            Project project = new Project()
            {
                Id = 1,
                Title = "Test Title",
                Description = "Test Description"
            };

            //act
            var result = await controller.EditProject(project!) as ViewResult;
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

            mockProject.Verify(x => x.GetAllPicturesOfSpecificProject(It.IsAny<int>()), Times.Once());
        }
        #endregion


    }
}
