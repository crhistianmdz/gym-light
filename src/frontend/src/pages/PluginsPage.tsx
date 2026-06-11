import React, { useState, useEffect } from 'react';
import { Box, Typography, Card, CardContent, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Switch, Alert, CircularProgress } from '@mui/material';
import { useAuth } from '@/contexts/AuthContext';
import { Navigate } from 'react-router-dom';
import { getPlugins, enablePlugin, disablePlugin, type PluginResponse } from '@/services/pluginService';

const PluginsPage: React.FC = () => {
  const { user } = useAuth();
  if (!user || (user.role !== 'Owner' && user.role !== 'Admin')) {
    return <Navigate to="/" replace />;
  }

  const [plugins, setPlugins] = useState<PluginResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [toggling, setToggling] = useState<string | null>(null);

  const fetchPlugins = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getPlugins();
      setPlugins(data);
    } catch (err) {
      setError('Error al cargar los plugins.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPlugins();
  }, []);

  const handleToggle = async (plugin: PluginResponse) => {
    setToggling(plugin.id);
    try {
      if (plugin.enabled) {
        await disablePlugin(plugin.id);
      } else {
        await enablePlugin(plugin.id);
      }
      await fetchPlugins();
    } catch (err) {
      setError(`Error al ${plugin.enabled ? 'deshabilitar' : 'habilitar'} el plugin.`);
    } finally {
      setToggling(null);
    }
  };

  if (loading) {
    return (
      <Box sx={{ p: 3, display: 'flex', justifyContent: 'center' }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        Gestión de Plugins
      </Typography>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      <Card>
        <CardContent>
          {plugins.length === 0 ? (
            <Typography color="text.secondary">
              No hay plugins instalados.
            </Typography>
          ) : (
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Nombre</TableCell>
                    <TableCell>Versión</TableCell>
                    <TableCell>Offline</TableCell>
                    <TableCell>Estado</TableCell>
                    <TableCell>Acciones</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {plugins.map((plugin) => (
                    <TableRow key={plugin.id}>
                      <TableCell>{plugin.name}</TableCell>
                      <TableCell>{plugin.version}</TableCell>
                      <TableCell>{plugin.offlineCapable ? 'Sí' : 'No'}</TableCell>
                      <TableCell>{plugin.enabled ? 'Habilitado' : 'Deshabilitado'}</TableCell>
                      <TableCell>
                        <Switch
                          checked={plugin.enabled}
                          onChange={() => handleToggle(plugin)}
                          disabled={toggling === plugin.id}
                        />
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </CardContent>
      </Card>
    </Box>
  );
};

export default PluginsPage;