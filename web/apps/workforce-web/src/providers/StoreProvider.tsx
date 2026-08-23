import { configureStore, createSlice, PayloadAction } from '@reduxjs/toolkit';
import { Provider } from 'react-redux';
import { ReactNode } from 'react';

// Example shell state slice for Phase 1A
interface AppShellState {
  sidebarOpen: boolean;
}

const initialState: AppShellState = {
  sidebarOpen: true,
};

export const appShellSlice = createSlice({
  name: 'appShell',
  initialState,
  reducers: {
    toggleSidebar: (state) => {
      state.sidebarOpen = !state.sidebarOpen;
    },
    setSidebarOpen: (state, action: PayloadAction<boolean>) => {
      state.sidebarOpen = action.payload;
    },
  },
});

export const { toggleSidebar, setSidebarOpen } = appShellSlice.actions;

export const store = configureStore({
  reducer: {
    appShell: appShellSlice.reducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;

export function StoreProvider({ children }: { children: ReactNode }) {
  return <Provider store={store}>{children}</Provider>;
}
