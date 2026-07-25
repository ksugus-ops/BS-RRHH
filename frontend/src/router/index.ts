import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/features/auth/LoginView.vue'),
    meta: { guestOnly: true },
  },
  {
    path: '/',
    component: () => import('@/shared/components/AppLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'home',
        component: () => import('@/features/dashboard/HomeView.vue'),
      },
      {
        path: 'employees',
        name: 'employees',
        component: () => import('@/features/employees/EmployeesListView.vue'),
        meta: { requiresAdmin: true },
      },
      {
        path: 'fichaje',
        name: 'time-tracking',
        component: () => import('@/features/time-tracking/TimeTrackingView.vue'),
      },
      {
        path: 'registros',
        name: 'time-admin',
        component: () => import('@/features/time-tracking/TimeAdminView.vue'),
        meta: { requiresAdmin: true },
      },
      {
        path: 'ausencias',
        name: 'absences',
        component: () => import('@/features/absences/AbsencesView.vue'),
      },
      {
        path: 'horarios',
        name: 'schedules',
        component: () => import('@/features/schedules/SchedulesView.vue'),
        meta: { requiresAdmin: true },
      },
      {
        path: 'calendario-laboral',
        name: 'work-calendar',
        component: () => import('@/features/work-calendar/WorkCalendarView.vue'),
        meta: { requiresAdmin: true },
      },
      {
        path: 'calendario-vacaciones',
        name: 'vacation-calendar',
        component: () => import('@/features/absences/VacationCalendarView.vue'),
        meta: { requiresAdmin: true },
      },
      {
        path: 'auditoria',
        name: 'audit',
        component: () => import('@/features/audit/AuditView.vue'),
        meta: { requiresAdmin: true },
      },
    ],
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('@/shared/components/NotFoundView.vue'),
  },
]

export const router = createRouter({
  history: createWebHistory(),
  routes,
})

// Guards de autenticación y autorización por rol.
router.beforeEach((to) => {
  const auth = useAuthStore()

  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }
  if (to.meta.requiresAdmin && !auth.isAdmin) {
    return { name: 'home' }
  }
  if (to.meta.guestOnly && auth.isAuthenticated) {
    return { name: 'home' }
  }
  return true
})
