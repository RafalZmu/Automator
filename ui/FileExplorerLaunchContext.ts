import { createContext } from 'react';
import type { FileExplorerLaunchRequest } from '../contracts/rpc';
export const FileExplorerLaunchContext = createContext<{ request: FileExplorerLaunchRequest | null; consume: () => void }>({ request: null, consume: () => {} });
