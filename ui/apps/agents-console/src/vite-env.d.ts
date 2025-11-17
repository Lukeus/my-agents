/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_NOTIFICATION_API_URL: string;
  readonly VITE_DEVOPS_API_URL: string;
  readonly VITE_TESTPLANNING_API_URL: string;
  readonly VITE_IMPLEMENTATION_API_URL: string;
  readonly VITE_SERVICEDESK_API_URL: string;
  readonly VITE_BIMCLASSIFICATION_API_URL: string;
  readonly VITE_ENABLE_HEALTH_CHECKS?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
