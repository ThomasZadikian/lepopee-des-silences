import api from './auth';
export interface InventoryItem 
{
    id: number; 
    itemId: number; 
    itemName: string; 
    quantity: number
}

export const inventoryApi = 
{
    getAll: () => api.get<InventoryItem[]>('/playerinventory'),
}