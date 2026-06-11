using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Extraction;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ProductEvidenceExtractorTests
{
    [TestMethod]
    public void Extracts_ProductName_From_Suministros_Title()
    {
        var evidence = ProductEvidenceExtractor.Extract(new ProductEvidenceInput
        {
            SourceUrl = "https://suministrosroca.uy/producto/placa-marmol-blanco-firenze/",
            SourceProfileId = "suministrosroca-uy-product",
            PageTitle = "Placa Marmol Blanco Firenze - ROCA - Suministros Roca",
            VisibleText = "Contacto | Productos | Pisos"
        });

        Assert.AreEqual("Placa Marmol Blanco Firenze", evidence.ProductName);
        Assert.AreEqual("ROCA", evidence.Brand);
    }

    [TestMethod]
    public void Extracts_ProductName_And_Category_From_Sodimac_Title_Text()
    {
        var evidence = ProductEvidenceExtractor.Extract(new ProductEvidenceInput
        {
            SourceUrl = "https://www.sodimac.com.uy/product/9065911/",
            SourceProfileId = "sodimac-product",
            PageTitle = "Piso flotante simil madera 6 mm Essen cafe claro mate interior 2.96 m2 - Sodimac.com.uy",
            VisibleText = "Pisos y Revestimientos | Servicio al cliente"
        });

        StringAssert.Contains(evidence.ProductName!, "Piso flotante");
        Assert.AreEqual("pisos/revestimientos", evidence.Category);
    }

    [TestMethod]
    public void Detects_Usd_Price_And_Currency_When_Visible()
    {
        var evidence = ProductEvidenceExtractor.Extract(new ProductEvidenceInput
        {
            PageTitle = "Piso flotante Essen - Sodimac.com.uy",
            VisibleText = "Precio USD 38.18 | sku=9065911 | Disponible"
        });

        Assert.AreEqual("38.18", evidence.Price);
        Assert.AreEqual("USD", evidence.Currency);
        Assert.AreEqual("9065911", evidence.Sku);
        Assert.AreEqual("high", evidence.ExtractionConfidence);
    }

    [TestMethod]
    public void Does_Not_Invent_Price_When_No_Price_Pattern_Exists()
    {
        var evidence = ProductEvidenceExtractor.Extract(new ProductEvidenceInput
        {
            PageTitle = "Placa Marmol Blanco Firenze - ROCA - Suministros Roca",
            VisibleText = "Producto publico sin precio visible"
        });

        Assert.IsNull(evidence.Price);
        Assert.IsNull(evidence.Currency);
        CollectionAssert.Contains(evidence.BlockedOrMissingFields.ToList(), "missing_price");
    }

    [TestMethod]
    public void Marks_MissingPrice_When_ProductName_Exists_Without_Price()
    {
        var evidence = ProductEvidenceExtractor.Extract(new ProductEvidenceInput
        {
            PageTitle = "Piso flotante Essen - Sodimac.com.uy",
            VisibleText = "Pisos y Revestimientos"
        });

        Assert.AreEqual("missing_price", evidence.ExtractionStatus);
        Assert.AreEqual("medium", evidence.ExtractionConfidence);
    }

    [TestMethod]
    public void Detects_Commercial_And_Auth_Signals_As_ReadOnly_Signals()
    {
        var evidence = ProductEvidenceExtractor.Extract(new ProductEvidenceInput
        {
            PageTitle = "Producto demo - Tienda",
            VisibleText = "Agregar al carrito | Comprar ahora | Pagar | Iniciar sesion | WhatsApp"
        });

        CollectionAssert.Contains(evidence.CartSignals.ToList(), "cart");
        CollectionAssert.Contains(evidence.BuySignals.ToList(), "buy");
        CollectionAssert.Contains(evidence.PaymentSignals.ToList(), "pay");
        CollectionAssert.Contains(evidence.LoginSignals.ToList(), "login");
        CollectionAssert.Contains(evidence.WhatsappSignals.ToList(), "whatsapp");
    }

    [TestMethod]
    public void Confidence_Medium_When_ProductName_Exists_But_Price_Missing()
    {
        var evidence = ProductEvidenceExtractor.Extract(new ProductEvidenceInput
        {
            PageTitle = "Placa Marmol Blanco Firenze - ROCA - Suministros Roca",
            VisibleText = "Revestimientos"
        });

        Assert.AreEqual("medium", evidence.ExtractionConfidence);
    }

    [TestMethod]
    public void Diagnostic_When_Blocking_Signals_Exist_Without_Product()
    {
        var evidence = ProductEvidenceExtractor.Extract(new ProductEvidenceInput
        {
            PageTitle = "",
            VisibleText = "Seleccione ubicacion | cookies | ventana emergente"
        });

        Assert.AreEqual("diagnostic", evidence.ExtractionStatus);
        Assert.AreEqual("diagnostic", evidence.ExtractionConfidence);
        CollectionAssert.Contains(evidence.CookieSignals.ToList(), "cookie");
        CollectionAssert.Contains(evidence.GeolocSignals.ToList(), "location");
    }
}
