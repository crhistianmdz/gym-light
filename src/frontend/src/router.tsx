import React from 'react';
import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom';
import { RoutinesPage } from './pages/RoutinesPage';
import { MemberDetail } from './pages/MemberDetail/MemberDetail';
import { MemberProgress } from './pages/MemberProgress/MemberProgress';import { SalesPage } from './pages/Sales';
import DashboardPage from './pages/DashboardPage';

const router = createBrowserRouter([
  { path: '/', element: <Navigate to="/members" replace /> },
  { path: '/members/:id/*', element: <MemberDetail /> },
  { path: '/members/:id/progress', element: <MemberProgress /> },
  { path: '/routines', element: <RoutinesPage /> },
  { path: '/sales', element: <SalesPage /> },
  { path: '/dashboard', element: <DashboardPage /> },
  { path: '*', element: <Navigate to="/" replace /> },
]);

const AppRouter: React.FC = () => <RouterProvider router={router} />;

export default AppRouter;