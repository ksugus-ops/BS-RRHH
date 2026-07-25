<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import WelcomeCard from '@/shared/components/WelcomeCard.vue'
import AdminDashboard from './AdminDashboard.vue'
import ClockPanel from '@/features/time-tracking/ClockPanel.vue'
import EmployeeMonthCard from './EmployeeMonthCard.vue'

const auth = useAuthStore()
</script>

<template>
  <div class="home">
    <template v-if="auth.isAdmin">
      <WelcomeCard
        :name="auth.user?.fullName ?? ''"
        :avatar-url="auth.user?.avatarUrl"
        subtitle="Panel de administración"
      />
      <AdminDashboard />
    </template>

    <template v-else>
      <div class="employee-row">
        <WelcomeCard
          class="wc"
          :name="auth.user?.fullName ?? ''"
          :avatar-url="auth.user?.avatarUrl"
          :subtitle="auth.user?.department ?? undefined"
        />
        <ClockPanel class="cp" :show-user="false" />
      </div>

      <EmployeeMonthCard />
    </template>
  </div>
</template>

<style scoped>
.home { display: flex; flex-direction: column; gap: 1.25rem; }
.employee-row {
  display: grid;
  grid-template-columns: minmax(260px, 340px) 1fr;
  gap: 1.25rem;
  align-items: stretch;
}
.wc { min-width: 0; }
.cp { min-width: 0; }
:deep(.cp.p-card) { height: 100%; }
@media (max-width: 860px) {
  .employee-row { grid-template-columns: 1fr; }
}
</style>
