import AppSectionState, {
  AppSectionDeleteState,
  AppSectionItemState,
  AppSectionSaveState,
} from 'App/State/AppSectionState';
import { IndexerCategory } from 'Indexer/Indexer';
import Application from 'typings/Application';
import Notification from 'typings/Notification';
import { UiSettings } from 'typings/UiSettings';

export interface AppProfileAppState
  extends AppSectionState<Application>,
    AppSectionDeleteState,
    AppSectionSaveState {}

export interface ApplicationAppState
  extends AppSectionState<Application>,
    AppSectionDeleteState,
    AppSectionSaveState {
  isTestingAll: boolean;
}

export interface IndexerCategoryAppState
  extends AppSectionState<IndexerCategory>,
    AppSectionDeleteState,
    AppSectionSaveState {}

export interface NotificationAppState
  extends AppSectionState<Notification>,
    AppSectionDeleteState {}

export type UiSettingsAppState = AppSectionItemState<UiSettings>;

interface SettingsAppState {
  appProfiles: AppProfileAppState;
  applications: ApplicationAppState;
  indexerCategories: IndexerCategoryAppState;
  notifications: NotificationAppState;
  ui: UiSettingsAppState;
}

export default SettingsAppState;
