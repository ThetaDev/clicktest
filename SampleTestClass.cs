using System.Collections.Generic;
using CommonInterfaces.Enums;
using DelfiCertUiTestDSL.DslObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.DslObjects;

namespace DelfiCertUiTestDSL {
    [TestClass]
    public class SampleTestClass : UiTestDslDelfiCert {
        [TestMethod]
        public void TestSelectionComboBox() {
            ChangeToDeliveriesTab();
            PrintComboBoxes();
            SearchFieldSelector.SelectItem("Delivery No");
            SearchFieldSelector.SelectedItem.ShouldRead("Delivery No");
        }
        
        [TestMethod]
        public void TestKorrekteHodefeltVises() {
            ChangeToRegisterTab();
            GuiLabels labels = GetLabels("Certificate_");
            labels.ShouldBeVisible("PoNo");
            labels.ShouldBeVisible("Manufacturer");
            labels.ShouldBeVisible("Manufacturer Cert. No");
            labels.ShouldBeVisible("CertNo");

            labels.ShouldNotBeDisplayed("N/A");
            Assert.AreEqual(9, labels.NumberOfVisible);
        }

        [TestMethod]
        public void TestRegistrering() {
            ChangeToRegisterTab();
            CreateNewUniqueIdentifier();

            //open image and create an arrow pointing to the middle
            Button("Open").Click();
            OpenFileDialog("Open").SelectFile(FindFileInAnyParentFolder(@"TestFiles\landscape.tif"));
            CertPageImage.LeftClickMouse();

            TextBoxes("Certificate_").AllShouldBeEmpty();
            TextBoxes("CertificateLine_").AllShouldBeEmpty();

            TextBox("Certificate_ManufacturerCertificateNo").Type("42"); 
            TextBox("Certificate_PoNo").Type(UniqueIdentifier);
            TextBox("CertificateLine_PoItemNo").Type("2");
            TextBox("CertificateLine_ArticleNo").Type("2");
            TextBox("CertificateLine_HeatNo").Type("2");

            TextBox("Certificate_ManufacturerCertificateNo").Type(UniqueIdentifier);

            TextBox("CertificateLine_Quantity").Type("1,5");
            TextBox("CertificateLine_Dimension").Type("test");

            //save the certificate
            Button("Register").Click();

            //find the certificate
            ChangeToCertificatesTab();
            SearchFieldSelector.SelectItem("PoNo");
            TextBox("txtSrcValue").Type(UniqueIdentifier);
            PressEnter();

            //verify fields are correctly saved
            Assert.AreEqual(1, CertificatesSearchResultGrid.RowCount, "Feil antall rader returnert");
            CertificatesSearchResultGrid.Cell(0, "PoNo").ShouldRead(UniqueIdentifier);
            //CertificatesSearchResultGrid.Cell(0, "Manufacturer Cert. No").ShouldRead(UniqueIdentifierToUse);
            CertificatesSearchResultGrid.Cell(0, "Quantity").ShouldRead("1,5");
            //todo resten av feltene?
        }
    }
}
