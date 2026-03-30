import React from 'react';
import { Tabs, Tab, Box } from '@mui/material';
import { useParams, Link, Route, Routes, useNavigate } from 'react-router-dom';
import { AnthropometryForm } from '@/components/AnthropometryForm/AnthropometryForm';
import { AnthropometryHistory } from '@/components/AnthropometryHistory/AnthropometryHistory';

const tabs = [
  { label: 'Antropometría', value: 'anthropometry', path: 'anthropometry' },
];

export const MemberDetail: React.FC = () => {
  const { id } = useParams<'id'>();
  const navigate = useNavigate();
  const [tabValue, setTabValue] = React.useState<string>(tabs[0].value);

  const handleTabChange = (_event: React.SyntheticEvent, newValue: string) => {
    setTabValue(newValue);
    navigate(`/members/${id}/${newValue}`);
  };

  return (
    <Box>
      <Tabs value={tabValue} onChange={handleTabChange} variant="scrollable" scrollButtons="auto">
        {tabs.map((tab) => (
          <Tab key={tab.value} label={tab.label} value={tab.value} component={Link} to={tab.path} />
        ))}
      </Tabs>

      <Routes>
        <Route path="anthropometry" element={
          <Box>
            <AnthropometryForm memberId={id!} />
            <AnthropometryHistory memberId={id!} />
          </Box>
        } />
      </Routes>
    </Box>
  );
};