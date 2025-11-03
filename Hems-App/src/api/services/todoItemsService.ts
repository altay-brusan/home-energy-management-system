import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  PaginatedListOfTodoItemBriefDto,
  CreateTodoItemCommand,
  UpdateTodoItemCommand,
  UpdateTodoItemDetailCommand,
  GetTodoItemsWithPaginationParams,
  UpdateTodoItemParams,
  UpdateTodoItemDetailParams,
  DeleteTodoItemParams,
} from '../types/todoItems.types';

export const todoItemsService = {
  // Get todo items with pagination
  getTodoItemsWithPagination: async (
    params: GetTodoItemsWithPaginationParams
  ): Promise<PaginatedListOfTodoItemBriefDto> => {
    const queryParams = new URLSearchParams();
    queryParams.append('ListId', params.ListId.toString());
    queryParams.append('PageNumber', params.PageNumber.toString());
    queryParams.append('PageSize', params.PageSize.toString());

    const url = `${ENDPOINTS.TODO_ITEMS.GET_ALL}?${queryParams.toString()}`;
    const response = await apiClient.get<PaginatedListOfTodoItemBriefDto>(url);
    return response.data;
  },

  // Create todo item
  createTodoItem: async (
    command: CreateTodoItemCommand
  ): Promise<number> => {
    const response = await apiClient.post<number>(ENDPOINTS.TODO_ITEMS.CREATE, command);
    return response.data;
  },

  // Update todo item
  updateTodoItem: async (
    params: UpdateTodoItemParams,
    command: UpdateTodoItemCommand
  ): Promise<void> => {
    const url = ENDPOINTS.TODO_ITEMS.UPDATE.replace(':id', params.id.toString());
    await apiClient.put<void>(url, command);
  },

  // Update todo item detail
  updateTodoItemDetail: async (
    params: UpdateTodoItemDetailParams,
    command: UpdateTodoItemDetailCommand
  ): Promise<void> => {
    const url = ENDPOINTS.TODO_ITEMS.UPDATE_DETAIL.replace(':id', params.id.toString());
    await apiClient.put<void>(url, command);
  },

  // Delete todo item
  deleteTodoItem: async (params: DeleteTodoItemParams): Promise<void> => {
    const url = ENDPOINTS.TODO_ITEMS.DELETE.replace(':id', params.id.toString());
    await apiClient.delete<void>(url);
  },
};
