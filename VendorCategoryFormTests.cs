using AutoMapper;
using Indotalent.Applications.VendorCategories;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Pages.VendorCategories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace TestProject2
{
    [TestFixture]
    public class VendorCategoryFormModelTests
    {
        private Mock<IMapper> _mapperMock;
        private VendorCategoryService _vendorCategoryService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private ApplicationDbContext _dbContext;
        private VendorCategoryFormModel _vendorCategoryFormModel;

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

            // Crear instancia de ApplicationDbContext
            _dbContext = new ApplicationDbContext(options);

            // Crear el servicio utilizando el dbContext real y los mocks de otras dependencias
            _vendorCategoryService = new VendorCategoryService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _vendorCategoryFormModel = new VendorCategoryFormModel(_mapperMock.Object, _vendorCategoryService);

            // Configurar TempData para evitar errores de referencia nula
            _vendorCategoryFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _vendorCategoryFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Disponer del contexto después de cada prueba
            _dbContext.Dispose();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevoVendorCategory()
        {
            // Arrange
            var newVendorCategoryModel = new VendorCategoryFormModel.VendorCategoryModel
            {
                Name = "Categoría A",
                Description = "Descripción de categoría de prueba"
            };

            _vendorCategoryFormModel.VendorCategoryForm = newVendorCategoryModel;
            _vendorCategoryFormModel.Action = "create";

            var mappedVendorCategory = new VendorCategory
            {
                RowGuid = Guid.NewGuid(),
                Name = newVendorCategoryModel.Name,
                Description = newVendorCategoryModel.Description
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newVendorCategoryModel;
            var expectedMappedResult = mappedVendorCategory;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<VendorCategory>(sourceModel))
                .Returns(expectedMappedResult);

            _vendorCategoryFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _vendorCategoryFormModel.OnPostAsync(newVendorCategoryModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./VendorCategoryForm?rowGuid={mappedVendorCategory.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert
            Assert.AreEqual("Success create new data.", _vendorCategoryFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");

            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EditarVendorCategory()
        {
            // Arrange
            var existingVendorCategory = new VendorCategory
            {
                RowGuid = Guid.NewGuid(),
                Name = "Categoría Existente",
                Description = "Descripción existente",
                IsNotDeleted = true
            };

            await _dbContext.VendorCategory.AddAsync(existingVendorCategory);
            await _dbContext.SaveChangesAsync();

            var editedVendorCategoryModel = new VendorCategoryFormModel.VendorCategoryModel
            {
                RowGuid = existingVendorCategory.RowGuid,
                Name = "Categoría Editada",
                Description = "Descripción actualizada"
            };

            _vendorCategoryFormModel.VendorCategoryForm = editedVendorCategoryModel;
            _vendorCategoryFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
    {
        { "action", "edit" }
    });

            _mapperMock
                .Setup(mapper => mapper.Map(editedVendorCategoryModel, existingVendorCategory))
                .Callback((VendorCategoryFormModel.VendorCategoryModel source, VendorCategory destination) =>
                {
                    destination.Name = source.Name;
                    destination.Description = source.Description;
                });

            _vendorCategoryFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _vendorCategoryFormModel.OnPostAsync(editedVendorCategoryModel);

            // Assert
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");
            string expectedUrl = $"./VendorCategoryForm?rowGuid={editedVendorCategoryModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;
            Assert.AreEqual("Success update existing data.", _vendorCategoryFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EliminarVendorCategory()
        {
            // Arrange
            var existingVendorCategory = new VendorCategory
            {
                RowGuid = Guid.NewGuid(),
                Name = "Categoría Existente",
                Description = "Descripción existente",
                IsNotDeleted = true
            };

            await _dbContext.VendorCategory.AddAsync(existingVendorCategory);
            await _dbContext.SaveChangesAsync();

            _vendorCategoryFormModel.VendorCategoryForm = new VendorCategoryFormModel.VendorCategoryModel
            {
                RowGuid = existingVendorCategory.RowGuid
            };

            _vendorCategoryFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
    {
        { "action", "delete" }
    });

            _vendorCategoryFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _vendorCategoryFormModel.OnPostAsync(_vendorCategoryFormModel.VendorCategoryForm);

            // Assert
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");

            string expectedUrl = "./VendorCategoryList";
            string actualUrl = redirectResult.Url;
            Assert.AreEqual("Success delete existing data.", _vendorCategoryFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            var deletedVendorCategory = await _dbContext.VendorCategory.SingleOrDefaultAsync(u => u.RowGuid == existingVendorCategory.RowGuid);
            Assert.IsNotNull(deletedVendorCategory, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedVendorCategory.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }

    }
}
