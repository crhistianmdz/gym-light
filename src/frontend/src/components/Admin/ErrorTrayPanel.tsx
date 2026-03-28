import { useState, useEffect } from 'react';
import { Table, TableBody, TableCell, TableHead, TableRow, Button } from '@mui/material';
import { db, ErrorQueueItem } from '@/db/gymflow.db';
import { syncService } from '@/services/syncService';

export function ErrorTrayPanel() {
  const [errorItems, setErrorItems] = useState<ErrorQueueItem[]>([]);

  useEffect(() => {
    const fetchErrors = async () => {
      setErrorItems(await db.error_queue.toArray());
    };

    fetchErrors();

    const handleError = () => {
      fetchErrors();
    };

    window.addEventListener('sync:item-failed', handleError);

    return () => {
      window.removeEventListener('sync:item-failed', handleError);
    };
  }, []);

  const retryError = async (guid: string) => {
    await syncService.retryFromErrorQueue(guid);
    setErrorItems(await db.error_queue.toArray());
  };

  const discardError = async (guid: string) => {
    await syncService.discardFromErrorQueue(guid);
    setErrorItems(await db.error_queue.toArray());
  };

  return (
    <Table>
      <TableHead>
        <TableRow>
          <TableCell>Tipo</TableCell>
          <TableCell>Timestamp</TableCell>
          <TableCell>Reintentos</TableCell>
          <TableCell>Último error</TableCell>
          <TableCell>Acciones</TableCell>
        </TableRow>
      </TableHead>
      <TableBody>
        {errorItems.map((item) => (
          <TableRow key={item.guid}>
            <TableCell>{item.type}</TableCell>
            <TableCell>{new Date(item.timestamp).toLocaleString()}</TableCell>
            <TableCell>{item.retryCount}</TableCell>
            <TableCell>{item.lastError}</TableCell>
            <TableCell>
              <Button onClick={() => retryError(item.guid)}>Reintentar</Button>
              <Button onClick={() => discardError(item.guid)}>Descartar</Button>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}