using AutoMapper;
using Indotalent.Applications.AdjustmentMinuss;
using Indotalent.Applications.NumberSequences;
using Indotalent.Applications.Products;
using Indotalent.Applications.Warehouses;
using Indotalent.Applications.InventoryTransactions;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Pages.NegativeAdjustments;
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
    public class NegativeAdjustmentFormTests
    {
        private Mock<IMapper> _mapperMock;
        private AdjustmentMinusService _adjustmentMinusService;
        private NumberSequenceService _numberSequenceService;
        private WarehouseService _warehouseService;
        private ProductService _productService;
        private InventoryTransactionService _inventoryTransactionService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private NegativeAdjustmentFormModel _negativeAdjustmentFormModel;
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
            _adjustmentMinusService = new AdjustmentMinusService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _numberSequenceService = new NumberSequenceService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _warehouseService = new WarehouseService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _productService = new ProductService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _inventoryTransactionService = new InventoryTransactionService(_warehouseService, _dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _negativeAdjustmentFormModel = new NegativeAdjustmentFormModel(_mapperMock.Object, _adjustmentMinusService, _numberSequenceService, _productService, _warehouseService, _inventoryTransactionService);

            // Configurar TempData para evitar errores de referencia nula
            _negativeAdjustmentFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _negativeAdjustmentFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Disponer del contexto después de cada prueba
            _dbContext.Dispose();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevoNegativeAdjustment()
        {
            // Arrange
            var newNegativeAdjustmentModel = new NegativeAdjustmentFormModel.AdjustmentMinusModel
            {
                AdjustmentDate = DateTime.Now,
                Description = "Testing Functionality"
            };

            _negativeAdjustmentFormModel.AdjustmentMinusForm = newNegativeAdjustmentModel;
            _negativeAdjustmentFormModel.Action = "create";

            var mappedNegativeAdjustment = new AdjustmentMinus
            {
                RowGuid = Guid.NewGuid(),
                AdjustmentDate = newNegativeAdjustmentModel.AdjustmentDate,
                Description = newNegativeAdjustmentModel.Description
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newNegativeAdjustmentModel;
            var expectedMappedResult = mappedNegativeAdjustment;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<AdjustmentMinus>(sourceModel))
                .Returns(expectedMappedResult);

            _negativeAdjustmentFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _negativeAdjustmentFormModel.OnPostAsync(newNegativeAdjustmentModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./NegativeAdjustmentForm?rowGuid={mappedNegativeAdjustment.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;


            // Assert
            Assert.AreEqual("Success create new data.", _negativeAdjustmentFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");

            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EditarNegativeAdjustment()
        {
            // Arrange 
            var newNegativeAdjustmentModel = new NegativeAdjustmentFormModel.AdjustmentMinusModel
            {
                AdjustmentDate = DateTime.Now,
                Description = "Testing Functionality"
            };

            _negativeAdjustmentFormModel.AdjustmentMinusForm = newNegativeAdjustmentModel;


            _negativeAdjustmentFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "create" }
            });

            var mappedNegativeAdjustment = new AdjustmentMinus
            {
                RowGuid = Guid.NewGuid(),
                AdjustmentDate = newNegativeAdjustmentModel.AdjustmentDate,
                Description = newNegativeAdjustmentModel.Description
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newNegativeAdjustmentModel;
            var expectedMappedResult = mappedNegativeAdjustment;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<AdjustmentMinus>(sourceModel))
                .Returns(expectedMappedResult);

            _negativeAdjustmentFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 1
            var createResult = await _negativeAdjustmentFormModel.OnPostAsync(newNegativeAdjustmentModel);
           
            var createdRowGuid = mappedNegativeAdjustment.RowGuid;

            var checkDate = DateTime.Now;

            // Arrange
            var editedNegativeAdjustmentModel = new NegativeAdjustmentFormModel.AdjustmentMinusModel
            {
                RowGuid = createdRowGuid,
                AdjustmentDate = checkDate,
                Description = "Testing Edit"
            };

            _negativeAdjustmentFormModel.AdjustmentMinusForm = editedNegativeAdjustmentModel;

            // Cambia el `Request.Query["action"]` a "edit" para simular edici�n
            _negativeAdjustmentFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            // Configura el mock para mapear el modelo de entrada editado a la entidad
            _mapperMock
                .Setup(mapper => mapper.Map(editedNegativeAdjustmentModel, mappedNegativeAdjustment))
                .Callback((NegativeAdjustmentFormModel.AdjustmentMinusModel source, AdjustmentMinus destination) =>
                {
                    destination.AdjustmentDate = source.AdjustmentDate;
                    destination.Description = source.Description;
                });

            // Act 2
            var editResult = await _negativeAdjustmentFormModel.OnPostAsync(editedNegativeAdjustmentModel);

            // Verifica si el `editResult` es de tipo RedirectResult y obt�n la URL
            var redirectResult = editResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");


            string expectedUrl = $"./NegativeAdjustmentForm?rowGuid={editedNegativeAdjustmentModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success update existing data.", _negativeAdjustmentFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");

            Assert.AreEqual(checkDate, mappedNegativeAdjustment.AdjustmentDate, "Campo Adjustment Date Actualizado.");
            Assert.AreEqual("Testing Edit", mappedNegativeAdjustment.Description, "Campo Description Actualizado.");

            // Usa Assert.AreEqual para un mensaje de error detallado
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

        }

        [Test]
        public async Task OnPostAsync_EliminarNegativeAdjustment()
        {
            // Arrange 
            var newNegativeAdjustmentModel = new NegativeAdjustmentFormModel.AdjustmentMinusModel
            {
                AdjustmentDate = DateTime.Now,
                Description = "Test Functionality"
            };

            _negativeAdjustmentFormModel.AdjustmentMinusForm = newNegativeAdjustmentModel;

          
            var mappedNegativeAdjustment = new AdjustmentMinus
            {
                RowGuid = Guid.NewGuid(),
                AdjustmentDate = newNegativeAdjustmentModel.AdjustmentDate,
                Description = newNegativeAdjustmentModel.Description,
                IsNotDeleted = true 
            };

            // Act 1
            await _dbContext.AdjustmentMinus.AddAsync(mappedNegativeAdjustment);
            await _dbContext.SaveChangesAsync();

            _negativeAdjustmentFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 2
            _negativeAdjustmentFormModel.AdjustmentMinusForm = new NegativeAdjustmentFormModel.AdjustmentMinusModel
            {
                RowGuid = mappedNegativeAdjustment.RowGuid
            };

            // Cambia el `Request.Query["action"]` a "delete" para simular la acción de eliminación
            _negativeAdjustmentFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            var deleteResult = await _negativeAdjustmentFormModel.OnPostAsync(_negativeAdjustmentFormModel.AdjustmentMinusForm);

            // Verifica que el resultado de `deleteResult` sea de tipo RedirectResult y obtiene la URL
            var redirectResult = deleteResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");

            // Define la URL esperada y la URL obtenida
            string expectedUrl = $"./NegativeAdjustmentList";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success delete existing data.", _negativeAdjustmentFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que el registro se haya marcado como eliminado en la base de datos
            var deletedNegativeAdjustment = await _dbContext.AdjustmentMinus
                .SingleOrDefaultAsync(u => u.RowGuid == mappedNegativeAdjustment.RowGuid);


            Assert.IsNotNull(deletedNegativeAdjustment, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedNegativeAdjustment.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }







    }







}