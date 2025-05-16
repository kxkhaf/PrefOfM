import {BrowserRouter, Routes, Route} from 'react-router-dom';
import {SearchProvider} from './contexts/SearchContext';
import SignUpForm from "./features/auth/SignUpForm.jsx";
import SignInForm from "./features/auth/SignInForm.jsx";
import EmailConfirmationPage from "./pages/auth/EmailConfirmationPage.jsx";
import ForgotPasswordPage from "./pages/auth/ForgotPasswordPage.jsx";
import PasswordResetPage from "./pages/auth/PasswordResetPage.jsx";
import EmailConfirmationRequestPage from "./pages/auth/EmailConfirmationRequestPage.jsx";
import MainLayout from "./layouts/MainLayout.jsx";
import HomePage from "./pages/music/HomePage.jsx";
import {TabProvider} from "./contexts/TabsContext.jsx";
import ProtectedRoute from "./components/ProtectedRoute/ProtectedRoutes.jsx";
import {EmotionProvider} from "./contexts/EmotionContext.jsx";
import {FastStartProvider} from "./contexts/FastStartContext.jsx";
import LoginLayout from "./layouts/LoginLayout.jsx";
import ProfilePage from "./pages/music/ProfilePage.jsx";
import SettingsPage from "./pages/music/SettingsPage.jsx";
import EmailChangeConfirmationPage from "./pages/music/EmailChangeConfirmationPage.jsx";
import SeedPage from "./pages/music/SeedPage.jsx";
import UserDataLayout from "./layouts/UserDataLayout.jsx";

function App() {
    return (
        <BrowserRouter>
            <SearchProvider>
                <TabProvider>
                    <EmotionProvider>
                        <FastStartProvider>
                            <Routes>
                                <Route path="/" element={<MainLayout/>}>
                                    <Route element={<ProtectedRoute/>}>
                                        <Route index element={<HomePage/>}/>
                                        <Route path="confirm-email-change" element={<EmailChangeConfirmationPage/>} />
                                    </Route>
                                </Route>
                                <Route path="/" element={<UserDataLayout/>}>
                                    <Route element={<ProtectedRoute/>}>
                                        <Route path="profile" element={<ProfilePage/>}/>
                                        <Route path="settings" element={<SettingsPage/>}/>
                                    </Route>
                                </Route>
                                <Route path="/" element={<LoginLayout/>}>
                                    <Route path="sign-up" element={<SignUpForm/>}/>
                                    <Route path="sign-in" element={<SignInForm/>}/>
                                    <Route path="confirm-email" element={<EmailConfirmationPage/>}/>
                                    <Route path="resend-email" element={<EmailConfirmationRequestPage/>}/>
                                    <Route path="forgot-password" element={<ForgotPasswordPage/>}/>
                                    <Route path="reset-password" element={<PasswordResetPage/>}/>
                                    <Route path="database/create/seed" element={<SeedPage/>} />
                                </Route>
                            </Routes>
                        </FastStartProvider>
                    </EmotionProvider>
                </TabProvider>
            </SearchProvider>
        </BrowserRouter>
    );
}

export default App;