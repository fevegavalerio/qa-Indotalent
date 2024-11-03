using AutoMapper;
using Indotalent.Applications.CustomerGroups;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Pages.CustomerGroups;
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
    public class CustomerGroupFormTests
    {
        private Mock<IMapper> _mapperMock;
        private CustomerGroupService _customerGroupService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private CustomerGroupFormModel _customerGroupFormModel;
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
            _customerGroupService = new CustomerGroupService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _customerGroupFormModel = new CustomerGroupFormModel(_mapperMock.Object, _customerGroupService);

            // Configurar TempData para evitar errores de referencia nula
            _customerGroupFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _customerGroupFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Disponer del contexto después de cada prueba
            _dbContext.Dispose();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevoCustomerGroup()
        {
            // Arrange
            var newCustomerGroupModel = new CustomerGroupFormModel.CustomerGroupModel
            {
                Name = "Test",
                Description = "Testing Functionality"
            };

            _customerGroupFormModel.CustomerGroupForm = newCustomerGroupModel;
            _customerGroupFormModel.Action = "create";

            var mappedCustomerGroup = new CustomerGroup
            {
                RowGuid = Guid.NewGuid(),
                Name = newCustomerGroupModel.Name,
                Description = newCustomerGroupModel.Description
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newCustomerGroupModel;
            var expectedMappedResult = mappedCustomerGroup;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<CustomerGroup>(sourceModel))
                .Returns(expectedMappedResult);

            _customerGroupFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _customerGroupFormModel.OnPostAsync(newCustomerGroupModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./CustomerGroupForm?rowGuid={mappedCustomerGroup.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;


            // Assert
            Assert.AreEqual("Success create new data.", _customerGroupFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");

            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EditarCustomerGroup()
        {
            // Arrange 
            var newCustomerGroupModel = new CustomerGroupFormModel.CustomerGroupModel
            {
                Name = "Test",
                Description = "Test Functionality"
            };

            _customerGroupFormModel.CustomerGroupForm = newCustomerGroupModel;


            _customerGroupFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "create" }
            });

            var mappedCustomerGroup = new CustomerGroup
            {
                RowGuid = Guid.NewGuid(),
                Name = newCustomerGroupModel.Name,
                Description = newCustomerGroupModel.Description
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newCustomerGroupModel;
            var expectedMappedResult = mappedCustomerGroup;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<CustomerGroup>(sourceModel))
                .Returns(expectedMappedResult);

            _customerGroupFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 1
            var createResult = await _customerGroupFormModel.OnPostAsync(newCustomerGroupModel);
           
            var createdRowGuid = mappedCustomerGroup.RowGuid;

            // Arrange
            var editedCustomerGroupModel = new CustomerGroupFormModel.CustomerGroupModel
            {
                RowGuid = createdRowGuid,
                Name = "Test edit",
                Description = "Edit functionality"
            };

            _customerGroupFormModel.CustomerGroupForm = editedCustomerGroupModel;

            // Cambia el `Request.Query["action"]` a "edit" para simular edici�n
            _customerGroupFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            // Configura el mock para mapear el modelo de entrada editado a la entidad
            _mapperMock
                .Setup(mapper => mapper.Map(editedCustomerGroupModel, mappedCustomerGroup))
                .Callback((CustomerGroupFormModel.CustomerGroupModel source, CustomerGroup destination) =>
                {
                    destination.Name = source.Name;
                    destination.Description = source.Description;
                });

            // Act 2
            var editResult = await _customerGroupFormModel.OnPostAsync(editedCustomerGroupModel);

            // Verifica si el `editResult` es de tipo RedirectResult y obt�n la URL
            var redirectResult = editResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");


            string expectedUrl = $"./CustomerGroupForm?rowGuid={editedCustomerGroupModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success update existing data.", _customerGroupFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");

            Assert.AreEqual("Test edit", mappedCustomerGroup.Name, "Campo Name Actualizado.");
            Assert.AreEqual("Edit functionality", mappedCustomerGroup.Description, "Campo Description Actualizado.");

            // Usa Assert.AreEqual para un mensaje de error detallado
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

        }

        [Test]
        public async Task OnPostAsync_EliminarCustomerGroup()
        {
            // Arrange 
            var newCustomerGroupModel = new CustomerGroupFormModel.CustomerGroupModel
            {
                Name = "Test",
                Description = "Test Functionality"
            };

            _customerGroupFormModel.CustomerGroupForm = newCustomerGroupModel;

          
            var mappedCustomerGroup = new CustomerGroup
            {
                RowGuid = Guid.NewGuid(),
                Name = newCustomerGroupModel.Name,
                Description = newCustomerGroupModel.Description,
                IsNotDeleted = true 
            };

            // Act 1
            await _dbContext.CustomerGroup.AddAsync(mappedCustomerGroup);
            await _dbContext.SaveChangesAsync();

            _customerGroupFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 2
            _customerGroupFormModel.CustomerGroupForm = new CustomerGroupFormModel.CustomerGroupModel
            {
                RowGuid = mappedCustomerGroup.RowGuid
            };

            // Cambia el `Request.Query["action"]` a "delete" para simular la acción de eliminación
            _customerGroupFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            var deleteResult = await _customerGroupFormModel.OnPostAsync(_customerGroupFormModel.CustomerGroupForm);

            // Verifica que el resultado de `deleteResult` sea de tipo RedirectResult y obtiene la URL
            var redirectResult = deleteResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");

            // Define la URL esperada y la URL obtenida
            string expectedUrl = $"./CustomerGroupList";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success delete existing data.", _customerGroupFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que el registro se haya marcado como eliminado en la base de datos
            var deletedCustomerGroup = await _dbContext.CustomerGroup
                .SingleOrDefaultAsync(u => u.RowGuid == mappedCustomerGroup.RowGuid);


            Assert.IsNotNull(deletedCustomerGroup, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedCustomerGroup.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }







    }







}