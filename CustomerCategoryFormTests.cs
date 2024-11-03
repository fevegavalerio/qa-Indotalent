using AutoMapper;
using Indotalent.Applications.CustomerCategories;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Pages.CustomerCategories;
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
    public class CustomerCategoryFormTests
    {
        private Mock<IMapper> _mapperMock;
        private CustomerCategoryService _customerCategoryService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private CustomerCategoryFormModel _customerCategoryFormModel;
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
            _customerCategoryService = new CustomerCategoryService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _customerCategoryFormModel = new CustomerCategoryFormModel(_mapperMock.Object, _customerCategoryService);

            // Configurar TempData para evitar errores de referencia nula
            _customerCategoryFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _customerCategoryFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Disponer del contexto después de cada prueba
            _dbContext.Dispose();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevoCustomerCategory()
        {
            // Arrange
            var newCustomerCategoryModel = new CustomerCategoryFormModel.CustomerCategoryModel
            {
                Name = "Test",
                Description = "Testing Functionality"
            };

            _customerCategoryFormModel.CustomerCategoryForm = newCustomerCategoryModel;
            _customerCategoryFormModel.Action = "create";

            var mappedCustomerCategory = new CustomerCategory
            {
                RowGuid = Guid.NewGuid(),
                Name = newCustomerCategoryModel.Name,
                Description = newCustomerCategoryModel.Description
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newCustomerCategoryModel;
            var expectedMappedResult = mappedCustomerCategory;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<CustomerCategory>(sourceModel))
                .Returns(expectedMappedResult);

            _customerCategoryFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _customerCategoryFormModel.OnPostAsync(newCustomerCategoryModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./CustomerCategoryForm?rowGuid={mappedCustomerCategory.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;


            // Assert
            Assert.AreEqual("Success create new data.", _customerCategoryFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");

            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EditarCustomerCategory()
        {
            // Arrange 
            var newCustomerCategoryModel = new CustomerCategoryFormModel.CustomerCategoryModel
            {
                Name = "Test",
                Description = "Test Functionality"
            };

            _customerCategoryFormModel.CustomerCategoryForm = newCustomerCategoryModel;


            _customerCategoryFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "create" }
            });

            var mappedCustomerCategory = new CustomerCategory
            {
                RowGuid = Guid.NewGuid(),
                Name = newCustomerCategoryModel.Name,
                Description = newCustomerCategoryModel.Description
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newCustomerCategoryModel;
            var expectedMappedResult = mappedCustomerCategory;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<CustomerCategory>(sourceModel))
                .Returns(expectedMappedResult);

            _customerCategoryFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 1
            var createResult = await _customerCategoryFormModel.OnPostAsync(newCustomerCategoryModel);
           
            var createdRowGuid = mappedCustomerCategory.RowGuid;

            // Arrange
            var editedCustomerCategoryModel = new CustomerCategoryFormModel.CustomerCategoryModel
            {
                RowGuid = createdRowGuid,
                Name = "Test edit",
                Description = "Edit functionality"
            };

            _customerCategoryFormModel.CustomerCategoryForm = editedCustomerCategoryModel;

            // Cambia el `Request.Query["action"]` a "edit" para simular edici�n
            _customerCategoryFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            // Configura el mock para mapear el modelo de entrada editado a la entidad
            _mapperMock
                .Setup(mapper => mapper.Map(editedCustomerCategoryModel, mappedCustomerCategory))
                .Callback((CustomerCategoryFormModel.CustomerCategoryModel source, CustomerCategory destination) =>
                {
                    destination.Name = source.Name;
                    destination.Description = source.Description;
                });

            // Act 2
            var editResult = await _customerCategoryFormModel.OnPostAsync(editedCustomerCategoryModel);

            // Verifica si el `editResult` es de tipo RedirectResult y obt�n la URL
            var redirectResult = editResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");


            string expectedUrl = $"./CustomerCategoryForm?rowGuid={editedCustomerCategoryModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success update existing data.", _customerCategoryFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");

            Assert.AreEqual("Test edit", mappedCustomerCategory.Name, "Campo Name Actualizado.");
            Assert.AreEqual("Edit functionality", mappedCustomerCategory.Description, "Campo Description Actualizado.");

            // Usa Assert.AreEqual para un mensaje de error detallado
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

        }

        [Test]
        public async Task OnPostAsync_EliminarCustomerCategory()
        {
            // Arrange 
            var newCustomerCategoryModel = new CustomerCategoryFormModel.CustomerCategoryModel
            {
                Name = "Test",
                Description = "Test Functionality"
            };

            _customerCategoryFormModel.CustomerCategoryForm = newCustomerCategoryModel;

          
            var mappedCustomerCategory = new CustomerCategory
            {
                RowGuid = Guid.NewGuid(),
                Name = newCustomerCategoryModel.Name,
                Description = newCustomerCategoryModel.Description,
                IsNotDeleted = true 
            };

            // Act 1
            await _dbContext.CustomerCategory.AddAsync(mappedCustomerCategory);
            await _dbContext.SaveChangesAsync();

            _customerCategoryFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 2
            _customerCategoryFormModel.CustomerCategoryForm = new CustomerCategoryFormModel.CustomerCategoryModel
            {
                RowGuid = mappedCustomerCategory.RowGuid
            };

            // Cambia el `Request.Query["action"]` a "delete" para simular la acción de eliminación
            _customerCategoryFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            var deleteResult = await _customerCategoryFormModel.OnPostAsync(_customerCategoryFormModel.CustomerCategoryForm);

            // Verifica que el resultado de `deleteResult` sea de tipo RedirectResult y obtiene la URL
            var redirectResult = deleteResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");

            // Define la URL esperada y la URL obtenida
            string expectedUrl = $"./CustomerCategoryList";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success delete existing data.", _customerCategoryFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que el registro se haya marcado como eliminado en la base de datos
            var deletedCustomerCategory = await _dbContext.CustomerCategory
                .SingleOrDefaultAsync(u => u.RowGuid == mappedCustomerCategory.RowGuid);


            Assert.IsNotNull(deletedCustomerCategory, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedCustomerCategory.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }







    }







}