import React from 'react';
import { Tabs, Tab, Box } from '@mui/material';
import { useParams, Link, Route, Routes, useNavigate } from 'react-router-dom';
import { AnthropometryForm } from '@/components/AnthropometryForm/AnthropometryForm';
import { AnthropometryHistory } from '@/components/AnthropometryHistory/AnthropometryHistory';
import { MemberProgress } from '@/pages/MemberProgress/MemberProgress';
import { FreezeMembershipPanel } from '@/components/MemberFreeze/FreezeMembershipPanel';
import { FreezeHistoryList } from '@/components/MemberFreeze/FreezeHistoryList';
import { CancelMembershipPanel } from '@/components/MemberCancel/CancelMembershipPanel';
import { useAuth } from '@/contexts/AuthContext';

const tabs = [
  { label: 'Antropometría', value: 'anthropometry', path: 'anthropometry' },
  { label: 'Progreso', value: 'progress', path: 'progress' },
  { label: 'Congelamiento', value: 'freeze', path: 'freeze' },
  { label: 'Cancelación', value: 'cancel', path: 'cancel' },
];

export const MemberDetail: React.FC = () => {
  const { id } = useParams<'id'>() ?? '';
  const navigate = useNavigate();
  const { user } = useAuth();
  const [tabValue, setTabValue] = React.useState<string>(tabs[0].value);

  const handleTabChange = (_event: React.SyntheticEvent, newValue: string) => {
    setTabValue(newValue);
    navigate(`/members/${id}/${newValue}`);
  };

  const userRole = user?.role;
  const isMemberOwner = user?.userId === id;

  return (
    <Box>
      <Tabs value={tabValue} onChange={handleTabChange} variant="scrollable" scrollButtons="auto">
        {tabs.map((tab) => {
          if (tab.value === 'freeze' && !['Admin', 'Owner'].includes(userRole ?? '')) {
            return null;
          }
          if (tab.value === 'cancel' && !['Admin', 'Owner'].includes(userRole ?? '') && !isMemberOwner) {
            return null;
          }
          return <Tab key={tab.value} label={tab.label} value={tab.value} component={Link} to={tab.path} />;
        })}
      </Tabs>

      <Routes>
        <Route
          path="anthropometry"
          element={
            <Box>
              <AnthropometryForm memberId={id!} />
              <AnthropometryHistory memberId={id!} />
            </Box>
          }
        />
        <Route path="progress" element={<MemberProgress />} />
        <Route
          path="freeze"
          element={
            <Box>
              <FreezeMembershipPanel
                memberId={id!}
                memberStatus={'Active'}
                membershipEndDate={''}
                onSuccess={() => console.log('Freeze success!')}
              />
              <FreezeHistoryList memberId={id!} />
            </Box>
          }
        />
        <Route
          path="cancel"
          element={
            <CancelMembershipPanel
              memberId={id!}
              memberStatus={'Active'}
              membershipEndDate={''}
              onSuccess={() => console.log('Cancel success!')}
            />
          }
        />
      </Routes>
    </Box>
  );
};
