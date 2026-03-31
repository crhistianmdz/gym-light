import React from 'react';
import { useParams } from 'react-router-dom';
import { Alert, Box, CircularProgress, Typography } from '@mui/material';
import { useAuth } from '@/contexts/AuthContext';
import { useMemberProgress } from '@/hooks/useMemberProgress';
import { ProgressChart } from '@/components/ProgressChart/ProgressChart';

const ALLOWED_ROLES = ['Trainer', 'Admin', 'Owner'];

/**
 * MemberProgress page — displays the ProgressChart for a given member.
 *
 * RBAC:
 *  - Member: can only view their own measurements (memberId === auth.userId).
 *  - Trainer, Admin, Owner: can view any member's measurements.
 */
export const MemberProgress: React.FC = () => {
  const { id: memberId }    = useParams<'id'>();
  const { user }            = useAuth();
  const { measurements, isLoading, error } = useMemberProgress(memberId ?? '');

  // RBAC guard
  const isMember      = user?.role === 'Member';
  const isOwnProfile  = user?.userId === memberId;
  const isAuthorized  = !isMember || isOwnProfile || ALLOWED_ROLES.includes(user?.role ?? '');

  if (!isAuthorized) {
    return (
      <Box mt={3}>
        <Alert severity="error">No tenés permiso para ver esta información.</Alert>
      </Box>
    );
  }

  return (
    <Box mt={2}>
      <Typography variant="h5" component="h1" gutterBottom>
        Progreso Físico
      </Typography>

      {isLoading && (
        <Box display="flex" justifyContent="center" mt={4}>
          <CircularProgress />
        </Box>
      )}

      {!isLoading && error && (
        <Alert severity="warning" sx={{ mt: 2 }}>
          {error}
        </Alert>
      )}

      {!isLoading && !error && (
        <ProgressChart measurements={measurements} />
      )}
    </Box>
  );
};
