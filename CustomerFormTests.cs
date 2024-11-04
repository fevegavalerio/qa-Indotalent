using AutoMapper;
using Indotalent.Applications.Customers;
using Indotalent.Applications.NumberSequences;
using Indotalent.Applications.CustomerGroups;
using Indotalent.Applications.CustomerCategories;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Infrastructures.Countries;
using Indotalent.Models.Entities;
using Indotalent.Pages.Customers;
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
    public class CustomerFormTests
    {
        private Mock<IMapper> _mapperMock;
        private CustomerService _customerService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private NumberSequenceService _numberSequenceService;
        private CustomerGroupService _customerGroupService;
        private CustomerCategoryService _customerCategoryService;
        private Mock<ICountryService> _countryServiceMock;
        private CustomerFormModel _customerFormModel;
        private ApplicationDbContext _dbContext;

        [SetUp]
        public void Setup()
        {
            // Crear mocks de las dependencias
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _auditColumnTransformerMock = new Mock<IAuditColumnTransformer>();
            _countryServiceMock = new Mock<ICountryService>();

            // Configurar DbContextOptions para ApplicationDbContext en modo de pruebas
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            // Crear y asignar la instancia de ApplicationDbContext
            _dbContext = new ApplicationDbContext(options);

            // Crear los servicios utilizando _dbContext real y los mocks de otras dependencias
            _customerService = new CustomerService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _numberSequenceService = new NumberSequenceService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _customerGroupService = new CustomerGroupService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _customerCategoryService = new CustomerCategoryService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _customerFormModel = new CustomerFormModel(_mapperMock.Object, _customerService, _numberSequenceService, _customerGroupService, _customerCategoryService, _countryServiceMock.Object);

            // Configurar TempData para evitar errores de referencia nula
            _customerFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _customerFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Disponer del contexto después de cada prueba
            _dbContext.Dispose();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevoCustomer()
        {
            // Arrange
            var newCustomerModel = new CustomerFormModel.CustomerModel
            {
                Name = "Test",
                CustomerGroupId = 1,
                CustomerCategoryId = 1
            };

            _customerFormModel.CustomerForm = newCustomerModel;
            _customerFormModel.Action = "create";

            var mappedCustomer = new Customer
            {
                RowGuid = Guid.NewGuid(),
                Name = newCustomerModel.Name,
                CustomerGroupId = newCustomerModel.CustomerGroupId,
                CustomerCategoryId = newCustomerModel.CustomerCategoryId
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newCustomerModel;
            var expectedMappedResult = mappedCustomer;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<Customer>(sourceModel))
                .Returns(expectedMappedResult);

            _customerFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _customerFormModel.OnPostAsync(newCustomerModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./CustomerForm?rowGuid={mappedCustomer.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;


            // Assert
            Assert.AreEqual("Success create new data.", _customerFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");

            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EditarCustomer()
        {
            // Arrange 
            var newCustomerModel = new CustomerFormModel.CustomerModel
            {
                Name = "Test",
                CustomerGroupId = 1,
                CustomerCategoryId = 1
            };

            _customerFormModel.CustomerForm = newCustomerModel;


            _customerFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "create" }
            });

            var mappedCustomer = new Customer
            {
                RowGuid = Guid.NewGuid(),
                Name = newCustomerModel.Name,
                CustomerGroupId = newCustomerModel.CustomerGroupId,
                CustomerCategoryId = newCustomerModel.CustomerCategoryId
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newCustomerModel;
            var expectedMappedResult = mappedCustomer;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<Customer>(sourceModel))
                .Returns(expectedMappedResult);

            _customerFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 1
            var createResult = await _customerFormModel.OnPostAsync(newCustomerModel);
           
            var createdRowGuid = mappedCustomer.RowGuid;

            // Arrange
            var editedCustomerModel = new CustomerFormModel.CustomerModel
            {
                RowGuid = createdRowGuid,
                Name = "Test Edit",
                CustomerGroupId = 2,
                CustomerCategoryId = 2
            };

            _customerFormModel.CustomerForm = editedCustomerModel;

            // Cambia el `Request.Query["action"]` a "edit" para simular edici�n
            _customerFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            // Configura el mock para mapear el modelo de entrada editado a la entidad
            _mapperMock
                .Setup(mapper => mapper.Map(editedCustomerModel, mappedCustomer))
                .Callback((CustomerFormModel.CustomerModel source, Customer destination) =>
                {
                    destination.Name = source.Name;
                    destination.CustomerGroupId = source.CustomerGroupId;
                    destination.CustomerCategoryId = source.CustomerCategoryId;
                });

            // Act 2
            var editResult = await _customerFormModel.OnPostAsync(editedCustomerModel);

            // Verifica si el `editResult` es de tipo RedirectResult y obt�n la URL
            var redirectResult = editResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");


            string expectedUrl = $"./CustomerForm?rowGuid={editedCustomerModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success update existing data.", _customerFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");

            Assert.AreEqual("Test Edit", mappedCustomer.Name, "Campo Name Actualizado.");
            Assert.AreEqual(2, mappedCustomer.CustomerGroupId, "Campo Customer Group Actualizado.");
            Assert.AreEqual(2, mappedCustomer.CustomerCategoryId, "Campo Customer Category Actualizado.");

            // Usa Assert.AreEqual para un mensaje de error detallado
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

        }

        [Test]
        public async Task OnPostAsync_EliminarCustomer()
        {
            // Arrange 
            var newCustomerModel = new CustomerFormModel.CustomerModel
            {
                Name = "Test",
                CustomerGroupId = 1,
                CustomerCategoryId = 1
            };

            _customerFormModel.CustomerForm = newCustomerModel;

          
            var mappedCustomer = new Customer
            {
                RowGuid = Guid.NewGuid(),
                Name = newCustomerModel.Name,
                CustomerGroupId = newCustomerModel.CustomerGroupId,
                CustomerCategoryId = newCustomerModel.CustomerCategoryId,
                IsNotDeleted = true 
            };

            // Act 1
            await _dbContext.Customer.AddAsync(mappedCustomer);
            await _dbContext.SaveChangesAsync();

            _customerFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 2
            _customerFormModel.CustomerForm = new CustomerFormModel.CustomerModel
            {
                RowGuid = mappedCustomer.RowGuid
            };

            // Cambia el `Request.Query["action"]` a "delete" para simular la acción de eliminación
            _customerFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            var deleteResult = await _customerFormModel.OnPostAsync(_customerFormModel.CustomerForm);

            // Verifica que el resultado de `deleteResult` sea de tipo RedirectResult y obtiene la URL
            var redirectResult = deleteResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");

            // Define la URL esperada y la URL obtenida
            string expectedUrl = $"./CustomerList";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success delete existing data.", _customerFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que el registro se haya marcado como eliminado en la base de datos
            var deletedCustomer = await _dbContext.Customer
                .SingleOrDefaultAsync(u => u.RowGuid == mappedCustomer.RowGuid);


            Assert.IsNotNull(deletedCustomer, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedCustomer.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }







    }







}