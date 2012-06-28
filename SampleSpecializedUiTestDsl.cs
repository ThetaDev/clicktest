using System;
using System.Drawing;
using CommonInterfaces.Enums;
using DelfiCertUiTestDSL.DslObjects;
using Microsoft.Test.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL;
using UiClickTestDSL.DslObjects;

namespace DelfiCertUiTestDSL {
    [TestClass]
    public abstract class UiTestDslDelfiCert : UiTestDslCoreCommon {
        [TestInitialize]
        public void StartApplicationAndLogin() {
            Program.LaunchApplication();
            try {
                GetThisWindow();
                WaitWhileBusy();
                Window.SetFocus();
                if (Window.Current.Name == "DelfiCert Logon") {
                    Login();

                    WaitWhileBusy();
                    GetThisWindow(); //get the actual mainwindow
                }
            } catch (Exception ex) {
                Print("Exception when trying to log in user, usually happens when application already is open. ");
                PrintLine(ex.Message);
                //this exception comes when we connect to the application, and the user is already logged in.
            }
            WaitWhileBusy();
            Window.SetFocus();
        }

        protected virtual void Login() {
            TextBox("txtUserName").Type("delficert");
            TextBox("txtPassword").Type("passord");
            Button("Ok").Click();
        }

        protected GuiLabel StatusBarLabel { get { return Label("txtMessage"); } }

        protected GuiComboBox SearchFieldSelector { get { return ComboBox("cmbFieldList"); } }

        protected GuiDataGrid CertificatesSearchResultGrid { get { return DataGrid("SearchResult"); } }
        protected GuiDataGrid CertificateBasketGrid { get { return DataGrid("Basket"); } }

        protected GuiButton ClearCustomerButton { get { return ButtonByAutomationId("ClearCustomerBtn"); } }

        protected GuiImage CertPageImage { get { return Image("ActualCertPage"); } }

        public void ChangeToRegisterTab() {
            InvalidateCachedObjects();
            WaitWhileBusy();
            Tab("RegisterCertificateTab").Select();
        }

        public void ChangeToCertificatesTab() {
            InvalidateCachedObjects();
            WaitWhileBusy();
            Tab("SearchCertificateTab").Select();
        }

        public void VerifyViewCertificateTabIsOpen(string certNo) {
            InvalidateCachedObjects();
            WaitWhileBusy();
            var tabName = GUI.Helpers.AutomationIdGenerator.EditCertificateTab(certNo);
            Tab(tabName).VerifyIsCurrentTab();
        }

        public void OpenUserAdminTab() {
            InvalidateCachedObjects();
            WaitWhileBusy();
            TypeText("^u");
        }

        //a specialized user control we are using in our project
        public GuiIssuePresenter IssuePresenter(string name) { return GuiIssuePresenter.GetIssuePresenter(Window, name); }

        //a specialized user control we are using in our project
        protected GuiNavBar NavBar { get { return GuiNavBar.Find(Window); } }
    }
}
