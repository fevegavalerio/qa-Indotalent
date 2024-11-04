using AutoMapper;
using Indotalent.Applications.CustomerContacts;
using Indotalent.Applications.NumberSequences;
using Indotalent.Applications.Customers;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Pages.CustomerContacts;
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
    public class CustomerContactFormTests
    {
        private Mock<IMapper> _mapperMock;
        private CustomerContactService _customerContactService;
        private NumberSequenceService _numberSequenceService;
        private CustomerService _customerService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private CustomerContactFormModel _customerContactFormModel;
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
            _customerContactService = new CustomerContactService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _numberSequenceService = new NumberSequenceService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _customerService = new CustomerService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _customerContactFormModel = new CustomerContactFormModel(_mapperMock.Object, _customerContactService, _numberSequenceService, _customerService);

            // Configurar TempData para evitar errores de referencia nula
            _customerContactFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _customerContactFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Disponer del contexto después de cada prueba
            _dbContext.Dispose();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevoCustomerContact()
        {
            // Arrange
            var newCustomerContactModel = new CustomerContactFormModel.CustomerContactModel
            {
                Name = "Test",
                CustomerId = 1
            };

            _customerContactFormModel.CustomerContactForm = newCustomerContactModel;
            _customerContactFormModel.Action = "create";

            var mappedCustomerContact = new CustomerContact
            {
                RowGuid = Guid.NewGuid(),
                Name = newCustomerContactModel.Name,
                CustomerId = newCustomerContactModel.CustomerId
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newCustomerContactModel;
            var expectedMappedResult = mappedCustomerContact;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<CustomerContact>(sourceModel))
                .Returns(expectedMappedResult);

            _customerContactFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _customerContactFormModel.OnPostAsync(newCustomerContactModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./CustomerContactForm?rowGuid={mappedCustomerContact.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;


            // Assert
            Assert.AreEqual("Success create new data.", _customerContactFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");

            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EditarCustomerContact()
        {
            // Arrange 
            var newCustomerContactModel = new CustomerContactFormModel.CustomerContactModel
            {
                Name = "Test",
                CustomerId = 1
            };

            _customerContactFormModel.CustomerContactForm = newCustomerContactModel;


            _customerContactFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "create" }
            });

            var mappedCustomerContact = new CustomerContact
            {
                RowGuid = Guid.NewGuid(),
                Name = newCustomerContactModel.Name,
                CustomerId = newCustomerContactModel.CustomerId
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newCustomerContactModel;
            var expectedMappedResult = mappedCustomerContact;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<CustomerContact>(sourceModel))
                .Returns(expectedMappedResult);

            _customerContactFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 1
            var createResult = await _customerContactFormModel.OnPostAsync(newCustomerContactModel);
           
            var createdRowGuid = mappedCustomerContact.RowGuid;

            // Arrange
            var editedCustomerContactModel = new CustomerContactFormModel.CustomerContactModel
            {
                RowGuid = createdRowGuid,
                Name = "Test Edit",
                CustomerId = 2
            };

            _customerContactFormModel.CustomerContactForm = editedCustomerContactModel;

            // Cambia el `Request.Query["action"]` a "edit" para simular edici�n
            _customerContactFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            // Configura el mock para mapear el modelo de entrada editado a la entidad
            _mapperMock
                .Setup(mapper => mapper.Map(editedCustomerContactModel, mappedCustomerContact))
                .Callback((CustomerContactFormModel.CustomerContactModel source, CustomerContact destination) =>
                {
                    destination.Name = source.Name;
                    destination.CustomerId = source.CustomerId;
                });

            // Act 2
            var editResult = await _customerContactFormModel.OnPostAsync(editedCustomerContactModel);

            // Verifica si el `editResult` es de tipo RedirectResult y obt�n la URL
            var redirectResult = editResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");


            string expectedUrl = $"./CustomerContactForm?rowGuid={editedCustomerContactModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success update existing data.", _customerContactFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");

            Assert.AreEqual("Test Edit", mappedCustomerContact.Name, "Campo Name Actualizado.");
            Assert.AreEqual(2, mappedCustomerContact.CustomerId, "Campo Customer Actualizado.");

            // Usa Assert.AreEqual para un mensaje de error detallado
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

        }

        [Test]
        public async Task OnPostAsync_EliminarCustomerContact()
        {
            // Arrange 
            var newCustomerContactModel = new CustomerContactFormModel.CustomerContactModel
            {
                Name = "Test",
                CustomerId = 1
            };

            _customerContactFormModel.CustomerContactForm = newCustomerContactModel;

          
            var mappedCustomerContact = new CustomerContact
            {
                RowGuid = Guid.NewGuid(),
                Name = newCustomerContactModel.Name,
                CustomerId = newCustomerContactModel.CustomerId,
                IsNotDeleted = true 
            };

            // Act 1
            await _dbContext.CustomerContact.AddAsync(mappedCustomerContact);
            await _dbContext.SaveChangesAsync();

            _customerContactFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 2
            _customerContactFormModel.CustomerContactForm = new CustomerContactFormModel.CustomerContactModel
            {
                RowGuid = mappedCustomerContact.RowGuid
            };

            // Cambia el `Request.Query["action"]` a "delete" para simular la acción de eliminación
            _customerContactFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            var deleteResult = await _customerContactFormModel.OnPostAsync(_customerContactFormModel.CustomerContactForm);

            // Verifica que el resultado de `deleteResult` sea de tipo RedirectResult y obtiene la URL
            var redirectResult = deleteResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");

            // Define la URL esperada y la URL obtenida
            string expectedUrl = $"./CustomerContactList";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success delete existing data.", _customerContactFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que el registro se haya marcado como eliminado en la base de datos
            var deletedCustomerContact = await _dbContext.CustomerContact
                .SingleOrDefaultAsync(u => u.RowGuid == mappedCustomerContact.RowGuid);


            Assert.IsNotNull(deletedCustomerContact, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedCustomerContact.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }







    }







}