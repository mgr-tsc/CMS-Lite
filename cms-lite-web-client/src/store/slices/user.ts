import { createSlice } from "@reduxjs/toolkit";
import type { PayloadAction } from "@reduxjs/toolkit";
import type { User } from "../../types/auth";

const initialState: User = {
  id: "",
  firstName: "",
  lastName: "",
  email: "",
  tenant: { id: "", name: "" },
  isAuthenticated: false,
};

const userSlice = createSlice({
  name: "user",
  initialState,
  reducers: {
    logInUser: (
      state,
      action: PayloadAction<Omit<User, "isAuthenticated">>
    ) => {
      return { ...state, ...action.payload, isAuthenticated: true };
    },
    logOutUser: () => initialState,
  },
});
export const { logInUser, logOutUser } = userSlice.actions;
export default userSlice.reducer;
