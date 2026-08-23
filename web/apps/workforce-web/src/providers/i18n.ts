import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

// In a real application, you'd load these from JSON files
const resources = {
  en: {
    translation: {
      "welcome": "Welcome to ZainX",
      "loading": "Loading..."
    }
  },
  ar: {
    translation: {
      "welcome": "مرحباً بكم في ZainX",
      "loading": "جاري التحميل..."
    }
  }
};

i18n
  .use(initReactI18next)
  .init({
    resources,
    lng: "en", // default language
    fallbackLng: "en",
    interpolation: {
      escapeValue: false 
    }
  });

// Handle RTL direction when language changes
i18n.on('languageChanged', (lng) => {
  document.documentElement.dir = i18n.dir(lng);
  document.documentElement.lang = lng;
});

// Set initial direction
document.documentElement.dir = i18n.dir(i18n.language);
document.documentElement.lang = i18n.language;

export default i18n;
