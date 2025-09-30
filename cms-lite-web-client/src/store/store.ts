import { configureStore } from "@reduxjs/toolkit";
import userSlice from "./slices/user";
import directoryTreeSlice from "./slices/directoryTree";
import dashboardSlice from "./slices/dashboard";

const store = configureStore({
  reducer: {
    user: userSlice,
    directoryTree: directoryTreeSlice,
    dashboard: dashboardSlice,
  },
});

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch
export default store;
