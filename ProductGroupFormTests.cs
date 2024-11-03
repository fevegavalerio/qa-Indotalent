using AutoMapper;
using Indotalent.Applications.ProductGroups;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Pages.ProductGroups;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using OpenQA.Selenium.BiDi.Modules.BrowsingContext;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace TestProject1
{
    [TestFixture]
    public class ProductGroupFormTests
    {
        private Mock<IMapper> _mapperMock;
        private ProductGroupService _productGroupService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private ProductGroupFormModel _productGroupFormModel;
        private ApplicationDbContext _dbContext;

        [SetUp]
        public void Setup()
        {
            // Crear mocks de las dependencias
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _auditColumnTransformerMock = new Mock<IAuditColumnTransformer>();

            // Configurar DbContextOptions para ApplicationDbContext en modo de pruebas
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            // Crear y asignar la instancia de ApplicationDbContext
            _dbContext = new ApplicationDbContext(options);

            // Crear el servicio utilizando _dbContext real y los mocks de otras dependencias
            _productGroupService = new ProductGroupService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _productGroupFormModel = new ProductGroupFormModel(_mapperMock.Object, _productGroupService);

            // Configurar TempData para evitar errores de referencia nula
            _productGroupFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _productGroupFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }




        [Test]
        public async Task OnPostAsync_AgregarNuevoProductGroup()
        {
            // Arrange
            var newProductGroupModel = new ProductGroupFormModel.ProductGroupModel
            {
                Name = "Database",
                Description = "description test"
            };

            _productGroupFormModel.ProductGroupForm = newProductGroupModel;
            _productGroupFormModel.Action = "create";

            var mappedProductGroup = new ProductGroup
            {
                RowGuid = Guid.NewGuid(),
                Name = newProductGroupModel.Name,
                Description = newProductGroupModel.Description
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newProductGroupModel;
            var expectedMappedResult = mappedProductGroup;

            // Configura el mock para devolver el objeto esperado cuando se llame al método Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<ProductGroup>(sourceModel))
                .Returns(expectedMappedResult);

            _productGroupFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _productGroupFormModel.OnPostAsync(newProductGroupModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./ProductGroupForm?rowGuid={mappedProductGroup.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;


            // Assert
            Assert.AreEqual("Success create new data.", _productGroupFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");

            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }




        [Test]
        public async Task OnPostAsync_EditarProductGroup()
        {
            // Arrange 
            var newProductGroupModel = new ProductGroupFormModel.ProductGroupModel
            {
                Name = "Database",
                Description = "description test"
            };

            _productGroupFormModel.ProductGroupForm = newProductGroupModel;


            _productGroupFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "create" }
            });

            var mappedProductGroup = new ProductGroup
            {
                RowGuid = Guid.NewGuid(),
                Name = newProductGroupModel.Name,
                Description = newProductGroupModel.Description
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newProductGroupModel;
            var expectedMappedResult = mappedProductGroup;

            // Configura el mock para devolver el objeto esperado cuando se llame al método Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<ProductGroup>(sourceModel))
                .Returns(expectedMappedResult);

            _productGroupFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 1
            var createResult = await _productGroupFormModel.OnPostAsync(newProductGroupModel);
           
            var createdRowGuid = mappedProductGroup.RowGuid;

            // Arrange
            var editedProductGroupModel = new ProductGroupFormModel.ProductGroupModel
            {
                RowGuid = createdRowGuid,
                Name = "Database editado",
                Description = "description test"
            };

            _productGroupFormModel.ProductGroupForm = editedProductGroupModel;

            // Cambia el `Request.Query["action"]` a "edit" para simular edición
            _productGroupFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            // Configura el mock para mapear el modelo de entrada editado a la entidad
            _mapperMock
                .Setup(mapper => mapper.Map(editedProductGroupModel, mappedProductGroup))
                .Callback((ProductGroupFormModel.ProductGroupModel source, ProductGroup destination) =>
                {
                    destination.Name = source.Name;
                    destination.Description = source.Description;
                });

            // Act 2
            var editResult = await _productGroupFormModel.OnPostAsync(editedProductGroupModel);

            // Verifica si el `editResult` es de tipo RedirectResult y obtén la URL
            var redirectResult = editResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");


            string expectedUrl = $"./ProductGroupForm?rowGuid={editedProductGroupModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success update existing data.", _productGroupFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");

            Assert.AreEqual("Database editado", mappedProductGroup.Name, "Campo Name Actualizado.");
            Assert.AreEqual("description test", mappedProductGroup.Description, "Campo Description Actualizado.");

            // Usa Assert.AreEqual para un mensaje de error detallado
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

        }


        [Test]
        public async Task OnPostAsync_EliminarProductGroup()
        {
            // Arrange 
            var newProductGroupModel = new ProductGroupFormModel.ProductGroupModel
            {
                Name = "Database",
                Description = "description test"
            };

            _productGroupFormModel.ProductGroupForm = newProductGroupModel;

          
            var mappedProductGroup = new ProductGroup
            {
                RowGuid = Guid.NewGuid(),
                Name = newProductGroupModel.Name,
                Description = newProductGroupModel.Description,
                IsNotDeleted = true 
            };

            // Act 1
            await _dbContext.ProductGroup.AddAsync(mappedProductGroup);
            await _dbContext.SaveChangesAsync();

            _productGroupFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 2
            _productGroupFormModel.ProductGroupForm = new ProductGroupFormModel.ProductGroupModel
            {
                RowGuid = mappedProductGroup.RowGuid
            };

            // Cambia el `Request.Query["action"]` a "delete" para simular la acción de eliminación
            _productGroupFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            var deleteResult = await _productGroupFormModel.OnPostAsync(_productGroupFormModel.ProductGroupForm);

            // Verifica que el resultado de `deleteResult` sea de tipo RedirectResult y obtiene la URL
            var redirectResult = deleteResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");

            // Define la URL esperada y la URL obtenida
            string expectedUrl = $"./ProductGroupList";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success delete existing data.", _productGroupFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que el registro se haya marcado como eliminado en la base de datos
            var deletedProductGroup = await _dbContext.ProductGroup
                .SingleOrDefaultAsync(u => u.RowGuid == mappedProductGroup.RowGuid);


            Assert.IsNotNull(deletedProductGroup, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedProductGroup.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }






    }







}
