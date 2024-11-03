using AutoMapper;
using Indotalent.Applications.AdjustmentPluss;
using Indotalent.Applications.NumberSequences;
using Indotalent.Applications.Products;
using Indotalent.Applications.Warehouses;
using Indotalent.Applications.InventoryTransactions;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Pages.PositiveAdjustments;
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
    public class PositiveAdjustmentFormTests
    {
        private Mock<IMapper> _mapperMock;
        private AdjustmentPlusService _adjustmentPlusService;
        private NumberSequenceService _numberSequenceService;
        private WarehouseService _warehouseService;
        private ProductService _productService;
        private InventoryTransactionService _inventoryTransactionService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private PositiveAdjustmentFormModel _positiveAdjustmentFormModel;
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
            _adjustmentPlusService = new AdjustmentPlusService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _numberSequenceService = new NumberSequenceService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _warehouseService = new WarehouseService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _productService = new ProductService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _inventoryTransactionService = new InventoryTransactionService(_warehouseService, _dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _positiveAdjustmentFormModel = new PositiveAdjustmentFormModel(_mapperMock.Object, _adjustmentPlusService, _numberSequenceService, _productService, _warehouseService, _inventoryTransactionService);

            // Configurar TempData para evitar errores de referencia nula
            _positiveAdjustmentFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _positiveAdjustmentFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Disponer del contexto después de cada prueba
            _dbContext.Dispose();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevoPositiveAdjustment()
        {
            // Arrange
            var newPositiveAdjustmentModel = new PositiveAdjustmentFormModel.AdjustmentPlusModel
            {
                AdjustmentDate = DateTime.Now,
                Description = "Testing Functionality"
            };

            _positiveAdjustmentFormModel.AdjustmentPlusForm = newPositiveAdjustmentModel;
            _positiveAdjustmentFormModel.Action = "create";

            var mappedPositiveAdjustment = new AdjustmentPlus
            {
                RowGuid = Guid.NewGuid(),
                AdjustmentDate = newPositiveAdjustmentModel.AdjustmentDate,
                Description = newPositiveAdjustmentModel.Description
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newPositiveAdjustmentModel;
            var expectedMappedResult = mappedPositiveAdjustment;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<AdjustmentPlus>(sourceModel))
                .Returns(expectedMappedResult);

            _positiveAdjustmentFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _positiveAdjustmentFormModel.OnPostAsync(newPositiveAdjustmentModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./PositiveAdjustmentForm?rowGuid={mappedPositiveAdjustment.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;


            // Assert
            Assert.AreEqual("Success create new data.", _positiveAdjustmentFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");

            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }







    }







}